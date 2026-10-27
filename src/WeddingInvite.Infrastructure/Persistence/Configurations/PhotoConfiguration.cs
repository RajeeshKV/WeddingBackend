using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingInvite.Domain.Entities;

namespace WeddingInvite.Infrastructure.Persistence.Configurations;

public sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("photos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CloudinaryPublicId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ImageUrl).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(x => x.Wedding).WithMany(w => w.Photos).HasForeignKey(x => x.WeddingId);
        builder.HasIndex(x => new { x.WeddingId, x.Status });
    }
}
