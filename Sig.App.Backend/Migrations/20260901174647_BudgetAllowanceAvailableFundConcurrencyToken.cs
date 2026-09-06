using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <summary>
    /// CRCL-2677 — Migration volontairement vide, et c'est le résultat attendu : ne pas la supprimer.
    ///
    /// <para>
    /// <c>BudgetAllowance.AvailableFund</c> devient jeton de concurrence. Contrairement à une colonne
    /// <c>rowversion</c>, cela n'ajoute rien au schéma : le jeton est une colonne qui existe déjà, et
    /// la protection s'exprime uniquement dans le SQL généré par EF, dont le UPDATE porte désormais
    /// « WHERE Id = @id AND AvailableFund = @valeurLue ». Il n'y a donc rien à faire en base.
    /// </para>
    ///
    /// <para>
    /// Cette migration n'existe que pour que <c>AppDbContextModelSnapshot</c> reste le reflet exact du
    /// modèle. Sans elle, la prochaine migration — quel que soit son sujet — se générerait contre un
    /// instantané périmé et émettrait un <c>AlterColumn</c> parasite sur AvailableFund.
    /// </para>
    /// </summary>
    public partial class BudgetAllowanceAvailableFundConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Aucun changement de schéma : voir la note de classe.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Aucun changement de schéma : voir la note de classe.
        }
    }
}
