using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Accord.Domain.Migrations
{
    public partial class AddUserReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterDiscordChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    ModeratorDiscordChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    OpenedByUserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    OpenedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedByUserId = table.Column<decimal>(type: "decimal(20,0)", nullable: true),
                    ClosedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReports_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_UserReports_Users_OpenedByUserId",
                        column: x => x.OpenedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "UserThreadBlocks",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    BlockedByUserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    BlockedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserThreadBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserThreadBlocks_Users_BlockedByUserId",
                        column: x => x.BlockedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "UserThreadMessages",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    UserThreadId = table.Column<int>(type: "int", nullable: false),
                    UserReportId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    SentDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserThreadMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserThreadMessages_UserReports_UserReportId",
                        column: x => x.UserReportId,
                        principalTable: "UserReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_UserThreadMessages_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Type", "Value" },
                values: new object[] { 3, "False" });

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Type", "Value" },
                values: new object[] { 4, "" });

            migrationBuilder.InsertData(
                table: "RunOptions",
                columns: new[] { "Type", "Value" },
                values: new object[] { 5, "" });

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ClosedByUserId",
                table: "UserReports",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_OpenedByUserId",
                table: "UserReports",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserThreadBlocks_BlockedByUserId",
                table: "UserThreadBlocks",
                column: "BlockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserThreadMessages_AuthorUserId",
                table: "UserThreadMessages",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserThreadMessages_UserReportId",
                table: "UserThreadMessages",
                column: "UserReportId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserThreadBlocks");

            migrationBuilder.DropTable(
                name: "UserThreadMessages");

            migrationBuilder.DropTable(
                name: "UserReports");

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "RunOptions",
                keyColumn: "Type",
                keyValue: 5);
        }
    }
}
