using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingInvite.Domain.Entities;

namespace WeddingInvite.Infrastructure.Persistence.Configurations;

public sealed class SuperUserConfiguration : IEntityTypeConfiguration<SuperUser>
{
    public void Configure(EntityTypeBuilder<SuperUser> builder)
    {
        builder.ToTable("super_users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(x => x.Username).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.SecurityVersion).IsRequired();
    }
}
