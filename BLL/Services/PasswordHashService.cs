using System.Security.Cryptography;
using System.Text;

namespace BLL.Services;

public static class PasswordHashService
{
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool VerifyPassword(string password, string? passwordHash)
    {
        return string.Equals(HashPassword(password), passwordHash, StringComparison.OrdinalIgnoreCase);
    }
}
