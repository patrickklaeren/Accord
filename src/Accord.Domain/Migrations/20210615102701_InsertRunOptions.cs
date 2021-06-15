using Microsoft.EntityFrameworkCore.Migrations;

namespace Accord.Domain.Migrations
{
    public partial class InsertRunOptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RunOption",
                table: "RunOption");

            migrationBuilder.RenameTable(
                name: "RunOption",
                newName: "RunOptions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RunOptions",
                table: "RunOptions",
                column: "Type");

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Type", "Value" },
                values: new object[] { 0, "False" });

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Type", "Value" },
                values: new object[] { 1, "False" });

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Type", "Value" },
                values: new object[] { 2, "10" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameTable(
                name: "RunOptions",
                newName: "RunOption");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RunOption",
                table: "RunOption",
                column: "Type");
        }
    }
}
