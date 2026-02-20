using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_role_permissions_keycloak_role_id_http_method_allowed",
                table: "RolePermissions");            

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_keycloak_role_id_http_method_allowed",
                table: "RolePermissions",
                columns: new[] { "keycloak_role_id", "http_method", "allowed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_role_permissions_keycloak_role_id_http_method_allowed",
                table: "RolePermissions");            

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_keycloak_role_id_http_method_allowed",
                table: "RolePermissions",
                columns: new[] { "keycloak_role_id", "http_method", "allowed" },
                filter: "Allowed = false");
        }
    }
}
