using EventPlug.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPlug.Api.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SubscriptionTier).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.BillingCycleStart).HasColumnType("date").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
