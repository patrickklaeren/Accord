using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class SpamEnablingOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Key", "Type", "Value" },
                values: new object[,]
                {
                    { 12, 3, "true" },
                    { 13, 1, "60" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyValue: 13);
        }
    }
}
