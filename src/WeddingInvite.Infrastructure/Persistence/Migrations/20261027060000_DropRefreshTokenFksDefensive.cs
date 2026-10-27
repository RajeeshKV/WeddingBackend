using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingInvite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropRefreshTokenFksDefensive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO 
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_refresh_tokens_admin_users_UserId') THEN
                        ALTER TABLE refresh_tokens DROP CONSTRAINT FK_refresh_tokens_admin_users_UserId;
                    END IF;
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_refresh_tokens_super_users_UserId') THEN
                        ALTER TABLE refresh_tokens DROP CONSTRAINT FK_refresh_tokens_super_users_UserId;
                    END IF;
                END ;
            ");

            migrationBuilder.Sql(@"
                DO 
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_refresh_tokens_UserId') THEN
                        DROP INDEX IX_refresh_tokens_UserId;
                    END IF;
                END ;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_refresh_tokens_UserId ON refresh_tokens (\"UserId\");");
        }
    }
}
