using MoralCompass.Infrastructure.Domain.Common;

namespace MoralCompass.Infrastructure.Domain;

public class CoreThesis: AuditableEntity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
}