using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserHistories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserHistory_Users_AddedByUserId",
                table: "UserHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_UserHistory_Users_UserId",
                table: "UserHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserHistory",
                table: "UserHistory");

            migrationBuilder.RenameTable(
                name: "UserHistory",
                newName: "UserHistories");

            migrationBuilder.RenameIndex(
                name: "IX_UserHistory_UserId",
                table: "UserHistories",
                newName: "IX_UserHistories_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserHistory_AddedByUserId",
                table: "UserHistories",
                newName: "IX_UserHistories_AddedByUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserHistories",
                table: "UserHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserHistories_Users_AddedByUserId",
                table: "UserHistories",
                column: "AddedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserHistories_Users_UserId",
                table: "UserHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserHistories_Users_AddedByUserId",
                table: "UserHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_UserHistories_Users_UserId",
                table: "UserHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserHistories",
                table: "UserHistories");

            migrationBuilder.RenameTable(
                name: "UserHistories",
                newName: "UserHistory");

            migrationBuilder.RenameIndex(
                name: "IX_UserHistories_UserId",
                table: "UserHistory",
                newName: "IX_UserHistory_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserHistories_AddedByUserId",
                table: "UserHistory",
                newName: "IX_UserHistory_AddedByUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserHistory",
                table: "UserHistory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserHistory_Users_AddedByUserId",
                table: "UserHistory",
                column: "AddedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserHistory_Users_UserId",
                table: "UserHistory",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
