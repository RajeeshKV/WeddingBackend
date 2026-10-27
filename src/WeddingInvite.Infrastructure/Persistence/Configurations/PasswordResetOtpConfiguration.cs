using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingInvite.Domain.Entities;

namespace WeddingInvite.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetOtpConfiguration : IEntityTypeConfiguration<PasswordResetOtp>
{
    public void Configure(EntityTypeBuilder<PasswordResetOtp> builder)
    {
        builder.ToTable("password_reset_otps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OtpHash).IsRequired().HasMaxLength(128);
        builder.HasOne(x => x.AdminUser).WithMany().HasForeignKey(x => x.AdminUserId);
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
    }
}
