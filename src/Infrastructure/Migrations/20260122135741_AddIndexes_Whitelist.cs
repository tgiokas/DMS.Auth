using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes_Whitelist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_whitelist_type_value",
                table: "EmailWhitelist");            

            migrationBuilder.CreateIndex(
                name: "ix_email_whitelist_type_value",
                table: "EmailWhitelist",
                columns: new[] { "type", "value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_whitelist_type_value",
                table: "EmailWhitelist");            

            migrationBuilder.CreateIndex(
                name: "ix_email_whitelist_type_value",
                table: "EmailWhitelist",
                columns: new[] { "type", "value" });
        }
    }
}
