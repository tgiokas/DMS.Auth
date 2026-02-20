using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_role_permissions_keycloak_role_id",
                table: "RolePermissions");

            migrationBuilder.CreateIndex(
                name: "ix_users_is_deleted",
                table: "Users",
                column: "is_deleted",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_keycloak_role_id_http_method_allowed",
                table: "RolePermissions",
                columns: new[] { "keycloak_role_id", "http_method", "allowed" },
                filter: "Allowed = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_is_deleted",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "ix_role_permissions_keycloak_role_id_http_method_allowed",
                table: "RolePermissions");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_keycloak_role_id",
                table: "RolePermissions",
                column: "keycloak_role_id");
        }
    }
}
