using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReportBlocks");

            migrationBuilder.DropTable(
                name: "UserReportMessages");

            migrationBuilder.DropTable(
                name: "UserReports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserReportBlocks",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    BlockedByUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    BlockedDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReportBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReportBlocks_Users_BlockedByUserId",
                        column: x => x.BlockedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClosedByUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    OpenedByUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ClosedDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InboxDiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    InboxDiscordMessageProxyWebhookId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    InboxDiscordMessageProxyWebhookToken = table.Column<string>(type: "text", nullable: false),
                    OpenedDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OutboxDiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OutboxDiscordMessageProxyWebhookId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OutboxDiscordMessageProxyWebhookToken = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReports_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserReports_Users_OpenedByUserId",
                        column: x => x.OpenedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReportMessages",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AuthorUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserReportId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    DiscordProxyMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    SentDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReportMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReportMessages_UserReports_UserReportId",
                        column: x => x.UserReportId,
                        principalTable: "UserReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserReportMessages_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserReportBlocks_BlockedByUserId",
                table: "UserReportBlocks",
                column: "BlockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReportMessages_AuthorUserId",
                table: "UserReportMessages",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReportMessages_UserReportId",
                table: "UserReportMessages",
                column: "UserReportId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ClosedByUserId",
                table: "UserReports",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_OpenedByUserId",
                table: "UserReports",
                column: "OpenedByUserId");
        }
    }
}
