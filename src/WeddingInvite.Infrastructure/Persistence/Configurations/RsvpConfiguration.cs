using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingInvite.Domain.Entities;

namespace WeddingInvite.Infrastructure.Persistence.Configurations;

public sealed class RsvpConfiguration : IEntityTypeConfiguration<Rsvp>
{
    public void Configure(EntityTypeBuilder<Rsvp> builder)
    {
        builder.ToTable("rsvps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GuestName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(x => x.Wedding).WithMany(w => w.Rsvps).HasForeignKey(x => x.WeddingId);
    }
}
