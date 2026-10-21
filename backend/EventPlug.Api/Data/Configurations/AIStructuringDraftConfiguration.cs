using EventPlug.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPlug.Api.Data.Configurations;

public class AIStructuringDraftConfiguration : IEntityTypeConfiguration<AIStructuringDraft>
{
    public void Configure(EntityTypeBuilder<AIStructuringDraft> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.RawInputText).IsRequired();
        builder.Property(x => x.StructuredOutput).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.Accepted).IsRequired();

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Event)
            .WithMany(x => x.AiStructuringDrafts)
            .HasForeignKey(x => new { x.OrgId, x.EventId })
            .HasPrincipalKey(x => new { x.OrgId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.AiStructuringDrafts)
            .HasForeignKey(x => new { x.OrgId, UserId = x.CreatedBy })
            .HasPrincipalKey(x => new { x.OrgId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
