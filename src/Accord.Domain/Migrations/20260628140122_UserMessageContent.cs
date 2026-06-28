using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UserMessageContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentsDetail",
                table: "UserMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "UserMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentsDetail",
                table: "UserMessages");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "UserMessages");
        }
    }
}
