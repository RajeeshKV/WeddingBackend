using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingInvite.Domain.Entities;

namespace WeddingInvite.Infrastructure.Persistence.Configurations;

public sealed class WeddingConfiguration : IEntityTypeConfiguration<Wedding>
{
    public void Configure(EntityTypeBuilder<Wedding> builder)
    {
        builder.ToTable("weddings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasOne(x => x.AdminUser).WithMany(a => a.Weddings).HasForeignKey(x => x.AdminUserId);
        builder.HasIndex(x => x.AdminUserId);
    }
}
