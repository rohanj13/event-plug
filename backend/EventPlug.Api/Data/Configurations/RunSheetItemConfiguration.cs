using EventPlug.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPlug.Api.Data.Configurations;

public class RunSheetItemConfiguration : IEntityTypeConfiguration<RunSheetItem>
{
    public void Configure(EntityTypeBuilder<RunSheetItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime);
        builder.Property(x => x.Day).HasDefaultValue(1).IsRequired();
        builder.Property(x => x.SortOrder).HasDefaultValue(0).IsRequired();
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_run_sheet_items_day_positive", "day >= 1");
            t.HasCheckConstraint("ck_run_sheet_items_end_after_start", "end_time IS NULL OR end_time >= start_time");
        });
        builder.HasIndex(x => new { x.EventId, x.Day, x.SortOrder });
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Event)
            .WithMany(x => x.RunSheetItems)
            .HasForeignKey(x => new { x.OrgId, x.EventId })
            .HasPrincipalKey(x => new { x.OrgId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OwnerUser)
            .WithMany(x => x.RunSheetItems)
            .HasForeignKey(x => new { x.OrgId, x.OwnerUserId })
            .HasPrincipalKey(x => new { x.OrgId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
