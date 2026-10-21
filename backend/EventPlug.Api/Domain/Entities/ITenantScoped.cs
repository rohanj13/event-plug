namespace EventPlug.Api.Domain.Entities;

public interface ITenantScoped
{
    Guid OrgId { get; set; }
}