using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Extensions
{
    /// <summary>
    /// CRCL-2677 — Mouvements d'enveloppe sûrs sous concurrence.
    ///
    /// <para>
    /// Le défaut d'origine : partout dans le code, créditer ou débiter une enveloppe s'écrit
    /// <c>budgetAllowance.AvailableFund += montant</c>. C'est un lire-modifier-écrire, et la lecture
    /// est séparée de l'écriture par tout le travail du handler — parfois des secondes. Deux
    /// opérations qui se chevauchent lisent le même solde, et la seconde écrit un total calculé
    /// depuis une valeur périmée : le mouvement de la première disparaît, sans erreur, alors que son
    /// TransactionLog (un INSERT, qui n'entre jamais en conflit) survit. D'où des journaux de
    /// remboursement sans crédit correspondant.
    /// </para>
    ///
    /// <para>
    /// Le correctif tient en deux moitiés, dont aucune ne suffit seule :
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///     <b>Rebaser le mouvement juste avant d'écrire.</b> Ce qu'un appelant exprime en écrivant
    ///     <c>+= 216</c> n'est pas « l'enveloppe vaut 216 », c'est « ajoute 216 à ce qu'elle vaut ».
    ///     Le mouvement (valeur voulue − valeur lue) reste donc valide quoi qu'ait fait un écrivain
    ///     concurrent ; il suffit de le réappliquer sur le solde réellement en base, relu au dernier
    ///     moment. C'est l'équivalent, au niveau du change tracker, de l'incrément atomique
    ///     <c>SET AvailableFund += @delta</c>, mais sans quitter le SaveChanges qui écrit aussi le
    ///     TransactionLog : les deux restent une seule transaction, donc un log ne peut plus survivre
    ///     à un crédit qui, lui, aurait échoué.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     <b>Prouver que personne n'a bougé entre-temps.</b> <c>AvailableFund</c> est déclaré jeton
    ///     de concurrence dans <see cref="AppDbContext"/> : l'UPDATE porte
    ///     « WHERE AvailableFund = &lt;valeur relue&gt; ». La fenêtre restante entre la relecture et
    ///     l'écriture ne peut donc plus produire une perte silencieuse — elle produit un
    ///     <see cref="DbUpdateConcurrencyException"/>, que l'on rebase et rejoue de la même façon.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// <b>Les deux montants de l'enveloppe sont rebasés ensemble</b>, et c'est essentiel :
    /// <c>MoveBudgetAllowance</c> et <c>EditBudgetAllowance</c> déplacent <c>OriginalFund</c> du même
    /// delta que <c>AvailableFund</c>. Ne rebaser que le solde disponible ferait diverger
    /// <c>OriginalFund − AvailableFund</c> — l'engagement de l'enveloppe, précisément ce
    /// qu'audite <see cref="BackgroundJobs.VerifyBudgetAllowanceReservations"/> — alors qu'avant ce
    /// correctif les deux se perdaient ensemble et restaient au moins cohérents entre eux. Un
    /// correctif partiel serait donc pire que pas de correctif du tout sur ce point.
    /// </para>
    ///
    /// <para>
    /// <b>Seul un débit peut être refusé.</b> Si l'opération concurrente a consommé les fonds sur
    /// lesquels un débit avait été autorisé, le rejouer mettrait l'enveloppe à découvert : on refuse
    /// (<see cref="BudgetAllowanceInsufficientFundException"/>), ce qui est la généralisation fidèle
    /// des gardes écrites site par site. Un crédit, lui, n'est jamais refusé — <b>même sur une
    /// enveloppe déjà à découvert</b>. Ces enveloppes existent en production (c'est ce que compte
    /// <c>VerifyBudgetAllowanceReservations.NegativeAvailableFundEnvelopes</c>) et leur refuser un
    /// remboursement bloquerait le retrait d'un participant sans jamais lui rendre son argent :
    /// exactement les chemins que ce ticket existe pour protéger.
    /// </para>
    /// </summary>
    public static class BudgetAllowanceConcurrencyExtensions
    {
        /// <summary>
        /// Nombre de SaveChanges tentés avant d'abandonner. La relecture préalable rend un conflit
        /// déjà rare ; au-delà de quelques rejeux, l'enveloppe est disputée par tant d'écrivains que
        /// boucler masquerait un problème au lieu de le résoudre.
        /// </summary>
        public const int MaxAttempts = 5;

        private const string AvailableFundName = nameof(BudgetAllowance.AvailableFund);
        private const string OriginalFundName = nameof(BudgetAllowance.OriginalFund);

        /// <summary>
        /// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> pour toute opération qui
        /// déplace des fonds d'enveloppe : rebase les mouvements sur les montants en base juste avant
        /// d'écrire, puis rejoue les conflits de concurrence résiduels. Tout conflit qui ne porte pas
        /// sur une enveloppe est relancé tel quel.
        /// </summary>
        public static async Task<int> SaveChangesWithBudgetAllowanceRetryAsync(
            this AppDbContext db, CancellationToken cancellationToken = default)
        {
            await RebaseOnPersistedFundsAsync(db, cancellationToken);

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException exception)
                {
                    if (attempt >= MaxAttempts) throw;
                    if (!await TryRebaseConflictsAsync(exception, cancellationToken)) throw;
                }
            }
        }

        /// <summary>
        /// Relit les montants réellement en base de chaque enveloppe sur le point d'être écrite, et y
        /// réapplique les mouvements voulus. La lecture est délibérément <c>AsNoTracking</c> et
        /// projetée : une requête suivie renverrait l'instance déjà en mémoire — donc les valeurs
        /// périmées que l'on cherche justement à corriger.
        /// </summary>
        private static async Task RebaseOnPersistedFundsAsync(AppDbContext db, CancellationToken cancellationToken)
        {
            var entries = db.ChangeTracker.Entries<BudgetAllowance>()
                .Where(x => x.State == EntityState.Modified)
                .Where(x => x.Property(AvailableFundName).IsModified || x.Property(OriginalFundName).IsModified)
                .ToList();

            if (entries.Count == 0) return;

            var ids = entries.Select(x => x.Entity.Id).ToList();
            var persistedFunds = await db.BudgetAllowances.AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, x.AvailableFund, x.OriginalFund })
                .ToDictionaryAsync(x => x.Id, x => new PersistedFunds(x.AvailableFund, x.OriginalFund), cancellationToken);

            var plan = new List<PlannedRebase>();

            foreach (var entry in entries)
            {
                if (!persistedFunds.TryGetValue(entry.Entity.Id, out var persisted)) continue;
                PlanRebase(entry, persisted, plan);
            }

            Apply(plan);
        }

        /// <summary>
        /// Rebase chaque enveloppe en conflit sur ses montants en base. Renvoie <c>false</c> dès qu'un
        /// conflit n'est pas rebasable, pour que l'appelant relance l'exception d'origine plutôt que
        /// d'inventer un résultat.
        /// </summary>
        private static async Task<bool> TryRebaseConflictsAsync(
            DbUpdateConcurrencyException exception, CancellationToken cancellationToken)
        {
            if (exception.Entries.Count == 0) return false;

            var plan = new List<PlannedRebase>();

            foreach (var entry in exception.Entries)
            {
                // Un conflit sur une autre entité, ou sur une enveloppe supprimée entre-temps, n'a pas
                // de mouvement à réappliquer : on ne sait pas le résoudre sans risquer d'écraser autre
                // chose.
                if (entry.Entity is not BudgetAllowance) return false;
                if (entry.State != EntityState.Modified) return false;

                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues == null) return false;

                var persisted = new PersistedFunds(
                    (decimal)databaseValues[AvailableFundName],
                    (decimal)databaseValues[OriginalFundName]);

                PlanRebase(entry, persisted, plan, exception);
            }

            Apply(plan);
            return true;
        }

        /// <summary>
        /// Calcule — sans rien modifier — les valeurs rebasées d'une enveloppe, et refuse ici, avant
        /// toute mutation, un débit que les fonds en base ne couvrent plus. Planifier d'abord et
        /// appliquer ensuite est ce qui rend le rebase tout-ou-rien : un refus sur la deuxième
        /// enveloppe d'un lot ne peut pas laisser la première à moitié rebasée.
        /// </summary>
        private static void PlanRebase(
            EntityEntry entry, PersistedFunds persisted, List<PlannedRebase> plan,
            DbUpdateConcurrencyException conflict = null)
        {
            var available = PlanProperty(entry.Property(AvailableFundName), persisted.AvailableFund);
            var original = PlanProperty(entry.Property(OriginalFundName), persisted.OriginalFund);

            // Un crédit passe toujours, y compris sur une enveloppe déjà à découvert : voir la note de
            // classe. Seul un débit que le solde en base ne couvre plus est refusé.
            if (available.Movement < 0m && available.Rebased < 0m)
            {
                throw new BudgetAllowanceInsufficientFundException(
                    $"Le mouvement d'enveloppe ne peut pas être appliqué : le solde en base " +
                    $"({persisted.AvailableFund}) ne couvre plus le débit de {-available.Movement} " +
                    $"autorisé sur la valeur lue ({available.Read}).",
                    conflict);
            }

            plan.Add(available);
            plan.Add(original);
        }

        private static PlannedRebase PlanProperty(PropertyEntry property, decimal persisted)
        {
            var read = (decimal)property.OriginalValue;
            var movement = (decimal)property.CurrentValue - read;

            return new PlannedRebase(property, read, movement, persisted, persisted + movement);
        }

        private static void Apply(List<PlannedRebase> plan)
        {
            foreach (var rebase in plan)
            {
                // La valeur lue devient celle de la base : le « WHERE AvailableFund = ... » du
                // SaveChanges vise l'état réel, et le mouvement est réappliqué par-dessus.
                rebase.Property.OriginalValue = rebase.Persisted;
                rebase.Property.CurrentValue = rebase.Rebased;
            }
        }

        private readonly record struct PersistedFunds(decimal AvailableFund, decimal OriginalFund);

        private readonly record struct PlannedRebase(
            PropertyEntry Property, decimal Read, decimal Movement, decimal Persisted, decimal Rebased);
    }

    /// <summary>
    /// Un débit d'enveloppe autorisé sur un solde qu'une opération concurrente a depuis consommé.
    /// Refuser est le seul comportement sûr : appliquer le mouvement quand même mettrait l'enveloppe à
    /// découvert, ce qu'aucune garde du domaine n'autorise. Un crédit n'emprunte jamais ce chemin.
    /// </summary>
    public class BudgetAllowanceInsufficientFundException : RequestValidationException
    {
        public BudgetAllowanceInsufficientFundException(string message, Exception innerException = null)
            : base(message)
        {
            ConcurrencyConflict = innerException;
        }

        /// <summary>
        /// Le conflit de concurrence à l'origine du refus, quand il y en a un — la course elle-même
        /// est le diagnostic utile, et <see cref="RequestValidationException"/> n'expose pas
        /// d'inner exception.
        /// </summary>
        public Exception ConcurrencyConflict { get; }
    }
}
