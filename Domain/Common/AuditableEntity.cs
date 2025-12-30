namespace MoralCompass.Infrastructure.Domain.Common;

public abstract class AuditableEntity: BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}