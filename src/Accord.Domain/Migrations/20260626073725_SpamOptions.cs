using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class SpamOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Key", "Type", "Value" },
                values: new object[,]
                {
                    { 10, 1, "30" },
                    { 11, 1, "3" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyValue: 11);
        }
    }
}
