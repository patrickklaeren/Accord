using Microsoft.EntityFrameworkCore.Migrations;

namespace Accord.Domain.Migrations
{
    public partial class AddUserReportsAgentRunOption : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Type", "Value" },
                values: new object[] { 6, "" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 6);
        }
    }
}
