using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingInvite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdminUserId",
                table: "weddings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "admin_users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "admin_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "admin_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "admin_user_frontend_urls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_user_frontend_urls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_user_frontend_urls_admin_users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "super_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    SecurityVersion = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_super_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weddings_AdminUserId",
                table: "weddings",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_user_frontend_urls_AdminUserId_Url",
                table: "admin_user_frontend_urls",
                columns: new[] { "AdminUserId", "Url" });

            migrationBuilder.CreateIndex(
                name: "IX_super_users_Email",
                table: "super_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_super_users_Username",
                table: "super_users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_weddings_admin_users_AdminUserId",
                table: "weddings",
                column: "AdminUserId",
                principalTable: "admin_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_weddings_admin_users_AdminUserId",
                table: "weddings");

            migrationBuilder.DropTable(
                name: "admin_user_frontend_urls");

            migrationBuilder.DropTable(
                name: "super_users");

            migrationBuilder.DropIndex(
                name: "IX_weddings_AdminUserId",
                table: "weddings");

            migrationBuilder.DropColumn(
                name: "AdminUserId",
                table: "weddings");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "admin_users");
        }
    }
}
