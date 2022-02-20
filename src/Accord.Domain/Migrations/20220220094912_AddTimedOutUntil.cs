using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accord.Domain.Migrations
{
    public partial class AddTimedOutUntil : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Xp",
                table: "Users");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimedOutUntil",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimedOutUntil",
                table: "Users");

            migrationBuilder.AddColumn<float>(
                name: "Xp",
                table: "Users",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
