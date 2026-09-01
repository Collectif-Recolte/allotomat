using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.Plugins.MediatR;
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
    ///     <b>Rebaser le delta juste avant d'écrire.</b> Ce qu'un appelant exprime en écrivant
    ///     <c>+= 216</c> n'est pas « l'enveloppe vaut 216 », c'est « ajoute 216 à ce qu'elle vaut ».
    ///     Le delta (valeur voulue − valeur lue) reste donc valide quoi qu'ait fait un écrivain
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
    /// Un rebase ne peut pas amener l'enveloppe sous zéro : l'opération concurrente a peut-être
    /// consommé les fonds sur lesquels le débit avait été autorisé. Dans ce cas l'opération est
    /// refusée (<see cref="BudgetAllowanceInsufficientFundException"/>) plutôt que réappliquée à
    /// l'aveugle. Cette règle est la généralisation fidèle des gardes déjà écrites site par site :
    /// toutes vérifient, sous une forme ou une autre, que le solde reste positif après le mouvement.
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

        private const string AvailableFundPropertyName = nameof(BudgetAllowance.AvailableFund);

        /// <summary>
        /// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> pour toute opération qui
        /// déplace des fonds d'enveloppe : rebase les deltas sur le solde en base juste avant
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
        /// Relit le solde réellement en base de chaque enveloppe sur le point d'être écrite, et y
        /// réapplique le delta voulu. La lecture est délibérément <c>AsNoTracking</c> et projetée :
        /// une requête suivie renverrait l'instance déjà en mémoire — donc la valeur périmée que l'on
        /// cherche justement à corriger.
        /// </summary>
        private static async Task RebaseOnPersistedFundsAsync(AppDbContext db, CancellationToken cancellationToken)
        {
            var entries = db.ChangeTracker.Entries<BudgetAllowance>()
                .Where(x => x.State == EntityState.Modified)
                .Where(x => x.Property(AvailableFundPropertyName).IsModified)
                .ToList();

            if (entries.Count == 0) return;

            var ids = entries.Select(x => x.Entity.Id).ToList();
            var persistedFunds = await db.BudgetAllowances.AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, x.AvailableFund })
                .ToDictionaryAsync(x => x.Id, x => x.AvailableFund, cancellationToken);

            foreach (var entry in entries)
            {
                if (!persistedFunds.TryGetValue(entry.Entity.Id, out var persisted)) continue;
                RebaseAvailableFund(entry.Property(AvailableFundPropertyName), persisted);
            }
        }

        /// <summary>
        /// Rebase chaque enveloppe en conflit sur sa valeur en base. Renvoie <c>false</c> dès qu'un
        /// conflit n'est pas rebasable, pour que l'appelant relance l'exception d'origine plutôt que
        /// d'inventer un résultat.
        /// </summary>
        private static async Task<bool> TryRebaseConflictsAsync(
            DbUpdateConcurrencyException exception, CancellationToken cancellationToken)
        {
            if (exception.Entries.Count == 0) return false;

            var conflicts = new List<(PropertyEntry Property, decimal Persisted)>();

            foreach (var entry in exception.Entries)
            {
                // Un conflit sur une autre entité, ou sur une enveloppe supprimée entre-temps, n'a pas
                // de delta à réappliquer : on ne sait pas le résoudre sans risquer d'écraser autre chose.
                if (entry.Entity is not BudgetAllowance) return false;
                if (entry.State != EntityState.Modified) return false;

                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues == null) return false;

                conflicts.Add((entry.Property(AvailableFundPropertyName),
                    (decimal)databaseValues[AvailableFundPropertyName]));
            }

            // Rien n'est modifié tant qu'on n'est pas certain de savoir résoudre tous les conflits :
            // un rebase partiel laisserait le change tracker dans un état ni d'avant ni d'après.
            foreach (var (property, persisted) in conflicts)
            {
                RebaseAvailableFund(property, persisted);
            }

            return true;
        }

        private static void RebaseAvailableFund(PropertyEntry availableFund, decimal persisted)
        {
            var wanted = (decimal)availableFund.CurrentValue;
            var read = (decimal)availableFund.OriginalValue;
            var movement = wanted - read;

            var rebased = persisted + movement;

            if (rebased < 0m)
            {
                throw new BudgetAllowanceInsufficientFundException(
                    $"Le mouvement d'enveloppe ne peut pas être appliqué : le solde en base ({persisted}) " +
                    $"ne couvre plus le débit de {-movement} autorisé sur la valeur lue ({read}).");
            }

            // La valeur lue devient celle de la base : le « WHERE AvailableFund = ... » du SaveChanges
            // vise l'état réel, et le delta est réappliqué par-dessus.
            availableFund.OriginalValue = persisted;
            availableFund.CurrentValue = rebased;
        }
    }

    /// <summary>
    /// Un débit d'enveloppe autorisé sur un solde qu'une opération concurrente a depuis consommé.
    /// Refuser est le seul comportement sûr : appliquer le delta quand même mettrait l'enveloppe à
    /// découvert, ce qu'aucune garde du domaine n'autorise.
    /// </summary>
    public class BudgetAllowanceInsufficientFundException : RequestValidationException
    {
        public BudgetAllowanceInsufficientFundException(string message) : base(message) { }
    }
}
