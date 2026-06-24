using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class Starboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RunOptions",
                table: "RunOptions");

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 0);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 7);

            migrationBuilder.AddColumn<int>(
                name: "Key",
                table: "RunOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RunOptions",
                table: "RunOptions",
                column: "Key");

            migrationBuilder.CreateTable(
                name: "StarboardEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StarredDiscordMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    StarredDiscordMessageChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    StarredDiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Stars = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarboardEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarboardEntries_Users_StarredDiscordUserId",
                        column: x => x.StarredDiscordUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StarboardEntryOutputs",
                columns: table => new
                {
                    StarboardEntryId = table.Column<int>(type: "integer", nullable: false),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarboardEntryOutputs", x => new { x.StarboardEntryId, x.DiscordMessageId, x.DiscordChannelId });
                    table.ForeignKey(
                        name: "FK_StarboardEntryOutputs_StarboardEntries_StarboardEntryId",
                        column: x => x.StarboardEntryId,
                        principalTable: "StarboardEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Key", "Type", "Value" },
                values: new object[,]
                {
                    { 0, 3, "False" },
                    { 1, 3, "False" },
                    { 2, 1, "10" },
                    { 7, 1, "3" },
                    { 8, 1, "3" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_StarboardEntries_StarredDiscordUserId",
                table: "StarboardEntries",
                column: "StarredDiscordUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StarboardEntryOutputs");

            migrationBuilder.DropTable(
                name: "StarboardEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RunOptions",
                table: "RunOptions");

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyColumnType: "integer",
                keyValue: 0);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyColumnType: "integer",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyColumnType: "integer",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyColumnType: "integer",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyColumnType: "integer",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "Key",
                table: "RunOptions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RunOptions",
                table: "RunOptions",
                column: "Type");

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Type", "Value" },
                values: new object[,]
                {
                    { 0, "False" },
                    { 1, "False" },
                    { 2, "10" },
                    { 3, "False" },
                    { 4, "" },
                    { 5, "" },
                    { 6, "" },
                    { 7, "3" }
                });
        }
    }
}
