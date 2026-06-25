using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class StarboardSelfStarringRunOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Key", "Type", "Value" },
                values: new object[] { 9, 3, "False" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyValue: 9);
        }
    }
}
