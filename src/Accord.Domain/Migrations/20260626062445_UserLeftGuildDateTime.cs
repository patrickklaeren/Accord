using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UserLeftGuildDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeftGuildDateTime",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeftGuildDateTime",
                table: "Users");
        }
    }
}
