using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeddingInvite.Domain.Entities;

namespace WeddingInvite.Infrastructure.Persistence.Configurations;

public sealed class WeddingEventConfiguration : IEntityTypeConfiguration<WeddingEvent>
{
    public void Configure(EntityTypeBuilder<WeddingEvent> builder)
    {
        builder.ToTable("wedding_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.HasOne(x => x.Wedding).WithMany(w => w.Events).HasForeignKey(x => x.WeddingId);
    }
}
