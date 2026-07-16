using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;

    public AuthenticationService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AuthenticationResultDto> AuthenticateAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        var loginName = usernameOrEmail.Trim();
        var user = await _userRepository.GetByUsernameAsync(loginName, cancellationToken)
            ?? await _userRepository.GetByEmailAsync(loginName, cancellationToken);

        if (user is null || !PasswordHashService.VerifyPassword(password, user.PasswordHash))
        {
            return AuthenticationResultDto.Failure("Tên đăng nhập/email hoặc mật khẩu không đúng.");
        }

        if (user.AccountStatus == AccountStatus.Pending || user.Role == UserRole.Pending)
        {
            return AuthenticationResultDto.Failure("Tài khoản của bạn chưa được quản trị viên phân quyền.");
        }

        if (user.AccountStatus == AccountStatus.Locked || !user.IsActive)
        {
            return AuthenticationResultDto.Failure("Tài khoản của bạn đã bị khóa.");
        }

        return AuthenticationResultDto.Success(new AuthenticatedUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        });
    }
}
