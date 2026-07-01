using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class DemocraticDownVoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Key", "Type", "Value" },
                values: new object[,]
                {
                    { 16, 3, "false" },
                    { 17, 1, "5" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyValue: 17);
        }
    }
}
