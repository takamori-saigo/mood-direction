using MoralCompass.Infrastructure.Domain.Common;
using MoralCompass.Infrastructure.Domain.Enums;

namespace MoralCompass.Infrastructure.Domain;

public class DiscussionItem : AuditableEntity
{
    public Guid? TopicId { get; set; }
    public DiscussionItemType Type { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }


    public ICollection<Comment> Comments { get; set; }
    
    public Topic Topic { get; set; }    

    public Guid AuthorId { get; set; } 
    public User Author { get; set; } 
}
