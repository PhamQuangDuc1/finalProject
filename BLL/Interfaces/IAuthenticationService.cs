using BLL.DTOs;

namespace BLL.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticationResultDto> AuthenticateAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default);
}
