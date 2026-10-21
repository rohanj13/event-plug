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

        builder.HasOne(x => x.Organization)
            .WithMany(x => x.EventTeams)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EventId, x.TeamId }).IsUnique();

        builder.HasOne(x => x.Event)
            .WithMany(x => x.EventTeams)
            .HasForeignKey(x => new { x.OrgId, x.EventId })
            .HasPrincipalKey(x => new { x.OrgId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Team)
            .WithMany(x => x.EventTeams)
            .HasForeignKey(x => new { x.OrgId, x.TeamId })
            .HasPrincipalKey(x => new { x.OrgId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApproverUser)
            .WithMany(x => x.EventTeamApprovals)
            .HasForeignKey(x => new { x.OrgId, UserId = x.ApproverUserId })
            .HasPrincipalKey(x => new { x.OrgId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
