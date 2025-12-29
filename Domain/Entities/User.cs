using MoralCompass.Infrastructure.Domain.Common;
using MoralCompass.Infrastructure.Domain.Enums;

namespace MoralCompass.Infrastructure.Domain;

public class User : AuditableEntity
{
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string Nickname { get; set; } = null!;
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public bool IsAdmin { get; set; }
    
    public ICollection<Topic> Topics { get; set; } = new List<Topic>();

    public string PasswordHash { get; set; } = null!;
}