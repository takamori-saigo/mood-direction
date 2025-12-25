using MoralCompass.Infrastructure.Domain.Common;

namespace MoralCompass.Infrastructure.Domain;

public class Topic : AuditableEntity
{
    public Guid CoreThesisId { get; set; }
    public string Title { get; set; }

    public Guid AuthorId { get; set; }

    public ICollection<DiscussionItem> DiscussionItems { get; set; }
    public CoreThesis CoreThesis { get; set; }
    
    public User Author { get; set; }    
}
