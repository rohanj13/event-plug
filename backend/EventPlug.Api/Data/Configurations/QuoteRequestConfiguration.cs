using EventPlug.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPlug.Api.Data.Configurations;

public class QuoteRequestConfiguration : IEntityTypeConfiguration<QuoteRequest>
{
    public void Configure(EntityTypeBuilder<QuoteRequest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.BriefSnapshot).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SentToEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.SentAt).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasOne(x => x.VendorEventLink)
            .WithMany(x => x.QuoteRequests)
            .HasForeignKey(x => x.VendorEventLinkId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
