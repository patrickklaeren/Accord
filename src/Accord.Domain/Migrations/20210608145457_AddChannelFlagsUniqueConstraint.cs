using Microsoft.EntityFrameworkCore.Migrations;

namespace Accord.Domain.Migrations
{
    public partial class AddChannelFlagsUniqueConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ChannelFlags_DiscordChannelId_Type",
                table: "ChannelFlags",
                columns: new[] { "DiscordChannelId", "Type" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChannelFlags_DiscordChannelId_Type",
                table: "ChannelFlags");
        }
    }
}
