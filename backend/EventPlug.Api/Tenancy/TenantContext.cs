using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? OrganizationId { get; private set; }
    public Guid? UserId { get; private set; }
    public OrgRole? OrganizationRole { get; private set; }

    public void SetMembership(Guid organizationId, Guid userId, OrgRole organizationRole)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID cannot be empty.", nameof(organizationId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (OrganizationId.HasValue &&
            (OrganizationId.Value != organizationId || UserId!.Value != userId))
        {
            throw new InvalidOperationException("The request membership cannot be changed once selected.");
        }

        OrganizationId = organizationId;
        UserId = userId;
        OrganizationRole = organizationRole;
    }
}
