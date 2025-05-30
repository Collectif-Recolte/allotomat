using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionExpirationNotificationSendDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExpired",
                table: "Subscriptions");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationNotificationSentDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpirationNotificationSentDate",
                table: "Subscriptions");

            migrationBuilder.AddColumn<bool>(
                name: "IsExpired",
                table: "Subscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
