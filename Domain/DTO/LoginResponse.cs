namespace Domain.DTO;

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public Guid UserId { get; set; }
    public string Nickname { get; set; } = null!;
    public string Email { get; set; } = null!;
}