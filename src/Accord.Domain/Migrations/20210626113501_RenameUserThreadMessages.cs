using Microsoft.EntityFrameworkCore.Migrations;

namespace Accord.Domain.Migrations
{
    public partial class RenameUserThreadMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserThreadBlocks_Users_BlockedByUserId",
                table: "UserThreadBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserThreadMessages_UserReports_UserReportId",
                table: "UserThreadMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_UserThreadMessages_Users_AuthorUserId",
                table: "UserThreadMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserThreadMessages",
                table: "UserThreadMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserThreadBlocks",
                table: "UserThreadBlocks");

            migrationBuilder.RenameTable(
                name: "UserThreadMessages",
                newName: "UserReportMessages");

            migrationBuilder.RenameTable(
                name: "UserThreadBlocks",
                newName: "UserReportBlocks");

            migrationBuilder.RenameIndex(
                name: "IX_UserThreadMessages_UserReportId",
                table: "UserReportMessages",
                newName: "IX_UserReportMessages_UserReportId");

            migrationBuilder.RenameIndex(
                name: "IX_UserThreadMessages_AuthorUserId",
                table: "UserReportMessages",
                newName: "IX_UserReportMessages_AuthorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserThreadBlocks_BlockedByUserId",
                table: "UserReportBlocks",
                newName: "IX_UserReportBlocks_BlockedByUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserReportMessages",
                table: "UserReportMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserReportBlocks",
                table: "UserReportBlocks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserReportBlocks_Users_BlockedByUserId",
                table: "UserReportBlocks",
                column: "BlockedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReportMessages_UserReports_UserReportId",
                table: "UserReportMessages",
                column: "UserReportId",
                principalTable: "UserReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReportMessages_Users_AuthorUserId",
                table: "UserReportMessages",
                column: "AuthorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserReportBlocks_Users_BlockedByUserId",
                table: "UserReportBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReportMessages_UserReports_UserReportId",
                table: "UserReportMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReportMessages_Users_AuthorUserId",
                table: "UserReportMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserReportMessages",
                table: "UserReportMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserReportBlocks",
                table: "UserReportBlocks");

            migrationBuilder.RenameTable(
                name: "UserReportMessages",
                newName: "UserThreadMessages");

            migrationBuilder.RenameTable(
                name: "UserReportBlocks",
                newName: "UserThreadBlocks");

            migrationBuilder.RenameIndex(
                name: "IX_UserReportMessages_UserReportId",
                table: "UserThreadMessages",
                newName: "IX_UserThreadMessages_UserReportId");

            migrationBuilder.RenameIndex(
                name: "IX_UserReportMessages_AuthorUserId",
                table: "UserThreadMessages",
                newName: "IX_UserThreadMessages_AuthorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserReportBlocks_BlockedByUserId",
                table: "UserThreadBlocks",
                newName: "IX_UserThreadBlocks_BlockedByUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserThreadMessages",
                table: "UserThreadMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserThreadBlocks",
                table: "UserThreadBlocks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserThreadBlocks_Users_BlockedByUserId",
                table: "UserThreadBlocks",
                column: "BlockedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserThreadMessages_UserReports_UserReportId",
                table: "UserThreadMessages",
                column: "UserReportId",
                principalTable: "UserReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserThreadMessages_Users_AuthorUserId",
                table: "UserThreadMessages",
                column: "AuthorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
