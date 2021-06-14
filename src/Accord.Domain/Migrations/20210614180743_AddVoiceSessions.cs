using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Accord.Domain.Migrations
{
    public partial class AddVoiceSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoiceConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    DiscordSessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscordChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    StartDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MinutesInVoiceChannel = table.Column<double>(type: "float", nullable: true),
                    HasBeenCountedToXp = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceConnections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceConnections_UserId",
                table: "VoiceConnections",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoiceConnections");
        }
    }
}
