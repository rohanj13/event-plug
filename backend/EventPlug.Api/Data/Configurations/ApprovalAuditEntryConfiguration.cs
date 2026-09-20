using EventPlug.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPlug.Api.Data.Configurations;

public class ApprovalAuditEntryConfiguration : IEntityTypeConfiguration<ApprovalAuditEntry>
{
    public void Configure(EntityTypeBuilder<ApprovalAuditEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.Property(x => x.Timestamp).IsRequired();

        builder.HasOne(x => x.Approval)
            .WithMany(x => x.AuditEntries)
            .HasForeignKey(x => x.ApprovalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Actor)
            .WithMany(x => x.ApprovalAuditEntries)
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
