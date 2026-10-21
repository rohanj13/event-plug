using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Tenancy;

public interface ITenantContext
{
    Guid? OrganizationId { get; }
    Guid? UserId { get; }
    OrgRole? OrganizationRole { get; }

    void SetMembership(Guid organizationId, Guid userId, OrgRole organizationRole);
}
