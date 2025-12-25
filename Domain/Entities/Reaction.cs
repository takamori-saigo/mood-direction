using MoralCompass.Infrastructure.Domain.Common;
using MoralCompass.Infrastructure.Domain.Enums;

namespace MoralCompass.Infrastructure.Domain;

public class Reaction : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ReactionTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    // +1 = лайк, -1 = дизлайк
    public int Value { get; set; }
}