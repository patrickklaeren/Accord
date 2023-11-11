using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RunOptions",
                columns: table => new
                {
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunOptions", x => x.Type);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    JoinedGuildDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsernameWithDiscriminator = table.Column<string>(type: "text", nullable: true),
                    Nickname = table.Column<string>(type: "text", nullable: true),
                    FirstSeenDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ParticipationRank = table.Column<int>(type: "integer", nullable: false),
                    ParticipationPoints = table.Column<int>(type: "integer", nullable: false),
                    ParticipationPercentile = table.Column<double>(type: "double precision", nullable: false),
                    TimedOutUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserHiddenChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ParentDiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHiddenChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserHiddenChannels_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMessages",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    SentDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RemindAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReminders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    OutboxDiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OutboxDiscordMessageProxyWebhookId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OutboxDiscordMessageProxyWebhookToken = table.Column<string>(type: "text", nullable: false),
                    InboxDiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    InboxDiscordMessageProxyWebhookId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    InboxDiscordMessageProxyWebhookToken = table.Column<string>(type: "text", nullable: false),
                    OpenedByUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OpenedDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedByUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    ClosedDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                name: "VoiceConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordSessionId = table.Column<string>(type: "text", nullable: false),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    StartDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MinutesInVoiceChannel = table.Column<double>(type: "double precision", nullable: true),
                    HasBeenCountedToXp = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "UserReportMessages",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordProxyMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserReportId = table.Column<int>(type: "integer", nullable: false),
                    AuthorUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    SentDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Type", "Value" },
                values: new object[,]
                {
                    { 0, "False" },
                    { 1, "False" },
                    { 2, "10" },
                    { 3, "False" },
                    { 4, "" },
                    { 5, "" },
                    { 6, "" },
                    { 7, "3" },
                    { 8, "False" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelFlags_DiscordChannelId_Type",
                table: "ChannelFlags",
                columns: new[] { "DiscordChannelId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_RoleId_Type",
                table: "Permissions",
                columns: new[] { "RoleId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId_Type",
                table: "Permissions",
                columns: new[] { "UserId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserHiddenChannels_UserId",
                table: "UserHiddenChannels",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessages_UserId",
                table: "UserMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReminders_UserId",
                table: "UserReminders",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_VoiceConnections_UserId",
                table: "VoiceConnections",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelFlags");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "RunOptions");

            migrationBuilder.DropTable(
                name: "UserHiddenChannels");

            migrationBuilder.DropTable(
                name: "UserMessages");

            migrationBuilder.DropTable(
                name: "UserReminders");

            migrationBuilder.DropTable(
                name: "UserReportBlocks");

            migrationBuilder.DropTable(
                name: "UserReportMessages");

            migrationBuilder.DropTable(
                name: "VoiceConnections");

            migrationBuilder.DropTable(
                name: "UserReports");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
