using EventPlug.Api.Domain.Entities;
using EventPlug.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPlug.Api.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.HasAlternateKey(x => new { x.OrgId, x.Id });

        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.EventDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_events_end_date_after_event_date",
            "end_date IS NULL OR end_date >= event_date"));
        builder.Property(x => x.Location).HasMaxLength(250).IsRequired();
        builder.Property(x => x.BudgetBand).HasMaxLength(120);
        builder.Property(x => x.BudgetAmount).HasPrecision(12, 2);
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_events_budget_amount_non_negative",
            "budget_amount IS NULL OR budget_amount >= 0"));
        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(EventScope.Team)
            .IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsShared).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Organization)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedEvents)
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
