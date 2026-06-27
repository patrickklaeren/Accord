using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RssFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RssFeeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "text", nullable: false),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LastFailedFetchResponse = table.Column<string>(type: "text", nullable: true),
                    NumberOfFailedFetches = table.Column<int>(type: "integer", nullable: false),
                    AddedDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastFetchDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextFetchDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RssFeeds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RssFeedPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    PublishedAtDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RssFeedId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RssFeedPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RssFeedPosts_RssFeeds_RssFeedId",
                        column: x => x.RssFeedId,
                        principalTable: "RssFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RssFeedPosts_RssFeedId",
                table: "RssFeedPosts",
                column: "RssFeedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RssFeedPosts");

            migrationBuilder.DropTable(
                name: "RssFeeds");
        }
    }
}
