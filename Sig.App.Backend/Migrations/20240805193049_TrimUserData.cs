using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class TrimUserData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Beneficiaries
                SET Firstname = NULLIF(LTRIM(RTRIM(Firstname)), ''),
                    Lastname = NULLIF(LTRIM(RTRIM(Lastname)), ''),
                    Email = NULLIF(LTRIM(RTRIM(Email)), ''),
                    Phone = NULLIF(LTRIM(RTRIM(Phone)), ''),
                    Address = NULLIF(LTRIM(RTRIM(Address)), ''),
                    Notes = NULLIF(LTRIM(RTRIM(Notes)), ''),
                    PostalCode = NULLIF(LTRIM(RTRIM(PostalCode)), ''),
                    ID1 = NULLIF(LTRIM(RTRIM(ID1)), ''),
                    ID2 = NULLIF(LTRIM(RTRIM(ID2)), '')");

            migrationBuilder.Sql(@"
                UPDATE BeneficiaryTypes
                SET Name = NULLIF(LTRIM(RTRIM(Name)), '')");

            migrationBuilder.Sql(@"
                UPDATE Markets
                SET Name = NULLIF(LTRIM(RTRIM(Name)), '')");

            migrationBuilder.Sql(@"
                UPDATE Organizations
                SET Name = NULLIF(LTRIM(RTRIM(Name)), '')");

            migrationBuilder.Sql(@"
                UPDATE ProductGroups
                SET Name = NULLIF(LTRIM(RTRIM(Name)), '')");

            migrationBuilder.Sql(@"
                UPDATE Projects
                SET Name = NULLIF(LTRIM(RTRIM(Name)), '')");

            migrationBuilder.Sql(@"
                UPDATE Subscriptions
                SET Name = NULLIF(LTRIM(RTRIM(Name)), '')");

            migrationBuilder.Sql(@"
                UPDATE UserProfiles
                SET FirstName = NULLIF(LTRIM(RTRIM(FirstName)), ''),
                    LastName = NULLIF(LTRIM(RTRIM(LastName)), '')");
        }
    }
}
