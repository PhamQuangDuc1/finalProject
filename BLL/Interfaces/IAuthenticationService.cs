using BLL.DTOs;

namespace BLL.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticatedUserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
}
