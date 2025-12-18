using System.Security.Cryptography;
using System.Text;

namespace MoralCompass.Controllers.Auth;

public static class HashPassword
{
    public static string GetHashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        
        return Convert.ToBase64String(hashedBytes);
    }
}