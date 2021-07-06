using Microsoft.EntityFrameworkCore.Migrations;

namespace Accord.Domain.Migrations
{
    public partial class UserReportsMessageProxy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InboxDiscordMessageProxyWebhookId",
                table: "UserReports",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InboxDiscordMessageProxyWebhookToken",
                table: "UserReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "OutboxDiscordMessageProxyWebhookId",
                table: "UserReports",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OutboxDiscordMessageProxyWebhookToken",
                table: "UserReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscordProxyMessageId",
                table: "UserReportMessages",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InboxDiscordMessageProxyWebhookId",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "InboxDiscordMessageProxyWebhookToken",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "OutboxDiscordMessageProxyWebhookId",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "OutboxDiscordMessageProxyWebhookToken",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "DiscordProxyMessageId",
                table: "UserReportMessages");
        }
    }
}
