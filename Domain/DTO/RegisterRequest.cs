using MoralCompass.Infrastructure.Domain.Enums;

namespace Domain.DTO;

public class RegisterRequest
{
    public string Nickname { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public int Age { get; set; }
    public Gender Gender { get; set; }
}