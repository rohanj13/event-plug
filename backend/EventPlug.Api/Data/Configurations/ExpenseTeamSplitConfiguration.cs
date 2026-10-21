using EventPlug.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPlug.Api.Data.Configurations;

public class ExpenseTeamSplitConfiguration : IEntityTypeConfiguration<ExpenseTeamSplit>
{
    public void Configure(EntityTypeBuilder<ExpenseTeamSplit> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.PortionAmount).HasPrecision(18, 2).IsRequired();

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Expense)
            .WithMany(x => x.ExpenseTeamSplits)
            .HasForeignKey(x => new { x.OrgId, x.ExpenseId })
            .HasPrincipalKey(x => new { x.OrgId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Team)
            .WithMany(x => x.ExpenseTeamSplits)
            .HasForeignKey(x => new { x.OrgId, x.TeamId })
            .HasPrincipalKey(x => new { x.OrgId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
