using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accord.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AwayWithDiscriminators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UsernameWithDiscriminator",
                table: "Users",
                newName: "Username");
            
            migrationBuilder.Sql("""
                                 UPDATE "Users"
                                 SET "Username" = regexp_replace("Username", '#[0-9]{4}$', '')
                                 WHERE "Username" ~ '#[0-9]{4}$';
                                 """);
            
            migrationBuilder.Sql("""
                                 UPDATE "Users"
                                 SET "LastSeenDateTime" = "FirstSeenDateTime"
                                 WHERE "LastSeenDateTime" = '-infinity'::timestamp;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Users",
                newName: "UsernameWithDiscriminator");
        }
    }
}
