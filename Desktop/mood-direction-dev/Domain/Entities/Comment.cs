using MoralCompass.Infrastructure.Domain.Common;

namespace MoralCompass.Infrastructure.Domain;

public class Comment : AuditableEntity
{
    public Guid DiscussionItemId { get; set; }
    public DiscussionItem DiscussionItem { get; set; } = null!;

    public string Content { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
}