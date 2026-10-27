using Microsoft.EntityFrameworkCore;
using WeddingInvite.Domain.Entities;

namespace WeddingInvite.Infrastructure.Persistence;

public sealed class WeddingInviteDbContext(DbContextOptions<WeddingInviteDbContext> options) : DbContext(options)
{
    public DbSet<SuperUser> SuperUsers => Set<SuperUser>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminUserFrontendUrl> AdminUserFrontendUrls => Set<AdminUserFrontendUrl>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<Wedding> Weddings => Set<Wedding>();
    public DbSet<WeddingEvent> WeddingEvents => Set<WeddingEvent>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeddingInviteDbContext).Assembly);
    }
}
