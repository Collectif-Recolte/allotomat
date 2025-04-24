using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class BlacklistedEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlacklistedEmails",
                columns: table => new
                {
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstAddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedCount = table.Column<int>(type: "int", nullable: false),
                    EmailSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmailSubject = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistedEmails", x => x.Email);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlacklistedEmails");
        }
    }
}
