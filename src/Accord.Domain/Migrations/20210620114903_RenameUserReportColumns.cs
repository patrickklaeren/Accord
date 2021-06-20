using Microsoft.EntityFrameworkCore.Migrations;

namespace Accord.Domain.Migrations
{
    public partial class RenameUserReportColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserThreadId",
                table: "UserThreadMessages");

            migrationBuilder.RenameColumn(
                name: "ReporterDiscordChannelId",
                table: "UserReports",
                newName: "OutboxDiscordChannelId");

            migrationBuilder.RenameColumn(
                name: "ModeratorDiscordChannelId",
                table: "UserReports",
                newName: "InboxDiscordChannelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OutboxDiscordChannelId",
                table: "UserReports",
                newName: "ReporterDiscordChannelId");

            migrationBuilder.RenameColumn(
                name: "InboxDiscordChannelId",
                table: "UserReports",
                newName: "ModeratorDiscordChannelId");

            migrationBuilder.AddColumn<int>(
                name: "UserThreadId",
                table: "UserThreadMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
