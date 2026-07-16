namespace BLL.DTOs;

public class AuthenticationResultDto
{
    public bool Succeeded { get; set; }

    public string? ErrorMessage { get; set; }

    public AuthenticatedUserDto? User { get; set; }

    public static AuthenticationResultDto Success(AuthenticatedUserDto user)
    {
        return new AuthenticationResultDto
        {
            Succeeded = true,
            User = user
        };
    }

    public static AuthenticationResultDto Failure(string message)
    {
        return new AuthenticationResultDto
        {
            Succeeded = false,
            ErrorMessage = message
        };
    }
}
