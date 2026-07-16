using BLL.DTOs;
using DAL.Entities;

namespace BLL.Interfaces;

public interface IUserManagementService
{
    Task<AccountDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CurrentUserDto currentUser, AccountFilterDto filter, CancellationToken cancellationToken = default);

    Task<AccountDto> AssignRoleAsync(CurrentUserDto currentUser, int userId, UserRole role, CancellationToken cancellationToken = default);

    Task<AccountDto> ActivateAccountAsync(CurrentUserDto currentUser, int userId, CancellationToken cancellationToken = default);

    Task<AccountDto> LockAccountAsync(CurrentUserDto currentUser, int userId, CancellationToken cancellationToken = default);
}
