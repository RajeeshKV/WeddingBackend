using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingInvite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropRefreshTokenFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_admin_users_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_super_users_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

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
    }
}
