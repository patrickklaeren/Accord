using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AutoUnmuting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VoiceAutoUnmuteAtDateTime",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Key", "Type", "Value" },
                values: new object[,]
                {
                    { 14, 3, "true" },
                    { 15, 1, "1440" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Key",
                keyValue: 15);

            migrationBuilder.DropColumn(
                name: "VoiceAutoUnmuteAtDateTime",
                table: "Users");
        }
    }
}
