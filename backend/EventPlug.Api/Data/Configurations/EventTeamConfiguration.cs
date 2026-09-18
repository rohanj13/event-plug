using EventPlug.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPlug.Api.Data.Configurations;

public class EventTeamConfiguration : IEntityTypeConfiguration<EventTeam>
{
    public void Configure(EntityTypeBuilder<EventTeam> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.SplitMethod).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.SplitValue).HasPrecision(18, 2);
        builder.Property(x => x.IsOwningTeam).IsRequired();

        builder.HasIndex(x => new { x.EventId, x.TeamId }).IsUnique();

        builder.HasOne(x => x.Event)
            .WithMany(x => x.EventTeams)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Team)
            .WithMany(x => x.EventTeams)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApproverUser)
            .WithMany(x => x.EventTeamApprovals)
            .HasForeignKey(x => x.ApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
