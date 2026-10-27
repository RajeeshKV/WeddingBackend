using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingInvite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_admin_users_AdminUserId",
                table: "refresh_tokens");

            migrationBuilder.RenameColumn(
                name: "AdminUserId",
                table: "refresh_tokens",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_AdminUserId",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_UserId");

            migrationBuilder.AddColumn<string>(
                name: "UserType",
                table: "refresh_tokens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_admin_users_UserId",
                table: "refresh_tokens",
                column: "UserId",
                principalTable: "admin_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_super_users_UserId",
                table: "refresh_tokens",
                column: "UserId",
                principalTable: "super_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_admin_users_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_super_users_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "refresh_tokens");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "refresh_tokens",
                newName: "AdminUserId");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_AdminUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_admin_users_AdminUserId",
                table: "refresh_tokens",
                column: "AdminUserId",
                principalTable: "admin_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
