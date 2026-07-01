using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class PromotionCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromotionCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ForUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ByUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    VouchedForByUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    VouchedByUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    ToDiscordRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    StartDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VoteThresholdRequired = table.Column<int>(type: "integer", nullable: false),
                    ClosedByUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionCampaigns_Users_ByUserId",
                        column: x => x.ByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionCampaigns_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PromotionCampaigns_Users_ForUserId",
                        column: x => x.ForUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionCampaigns_Users_VouchedByUserId",
                        column: x => x.VouchedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PromotionCampaignOutputs",
                columns: table => new
                {
                    PromotionCampaignId = table.Column<int>(type: "integer", nullable: false),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCampaignOutputs", x => new { x.PromotionCampaignId, x.DiscordMessageId, x.DiscordChannelId });
                    table.ForeignKey(
                        name: "FK_PromotionCampaignOutputs_PromotionCampaigns_PromotionCampai~",
                        column: x => x.PromotionCampaignId,
                        principalTable: "PromotionCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionCampaignVotes",
                columns: table => new
                {
                    PromotionCampaignId = table.Column<int>(type: "integer", nullable: false),
                    VotingUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Vote = table.Column<int>(type: "integer", nullable: false),
                    AtDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCampaignVotes", x => new { x.PromotionCampaignId, x.VotingUserId });
                    table.ForeignKey(
                        name: "FK_PromotionCampaignVotes_PromotionCampaigns_PromotionCampaign~",
                        column: x => x.PromotionCampaignId,
                        principalTable: "PromotionCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionCampaignVotes_Users_VotingUserId",
                        column: x => x.VotingUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_ByUserId",
                table: "PromotionCampaigns",
                column: "ByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_ClosedByUserId",
                table: "PromotionCampaigns",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_ForUserId",
                table: "PromotionCampaigns",
                column: "ForUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_VouchedByUserId",
                table: "PromotionCampaigns",
                column: "VouchedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaignVotes_VotingUserId",
                table: "PromotionCampaignVotes",
                column: "VotingUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionCampaignOutputs");

            migrationBuilder.DropTable(
                name: "PromotionCampaignVotes");

            migrationBuilder.DropTable(
                name: "PromotionCampaigns");
        }
    }
}
