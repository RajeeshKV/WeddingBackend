using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingInvite.Domain.Entities;

namespace WeddingInvite.Infrastructure.Persistence.Configurations;

public sealed class AdminUserFrontendUrlConfiguration : IEntityTypeConfiguration<AdminUserFrontendUrl>
{
    public void Configure(EntityTypeBuilder<AdminUserFrontendUrl> builder)
    {
        builder.ToTable("admin_user_frontend_urls");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.HasOne(x => x.AdminUser).WithMany(a => a.FrontendUrls).HasForeignKey(x => x.AdminUserId);
        builder.HasIndex(x => new { x.AdminUserId, x.Url });
    }
}
