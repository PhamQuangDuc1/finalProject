using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;

    public UserManagementService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AccountDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            throw new InvalidOperationException("Dữ liệu đăng ký không hợp lệ.");
        }

        var fullName = dto.FullName.Trim();
        var email = dto.Email.Trim();
        var username = dto.Username.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Họ và tên là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Tên đăng nhập là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 6 ký tự.");
        }

        if (await _userRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Email đã được sử dụng.");
        }

        if (await _userRepository.GetByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Tên đăng nhập đã được sử dụng.");
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            Username = username,
            PasswordHash = PasswordHashService.HashPassword(dto.Password),
            Role = UserRole.Pending,
            AccountStatus = AccountStatus.Pending,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);

        return ToDto(user);
    }

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(
        CurrentUserDto currentUser,
        AccountFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var accounts = await _userRepository.GetAllAsync(cancellationToken);
        var query = accounts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = query.Where(user =>
                user.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                user.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                user.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Role.HasValue)
        {
            query = query.Where(user => user.Role == filter.Role.Value);
        }

        if (filter.AccountStatus.HasValue)
        {
            query = query.Where(user => user.AccountStatus == filter.AccountStatus.Value);
        }

        return query.Select(ToDto).ToList();
    }

    public async Task<AccountDto> AssignRoleAsync(
        CurrentUserDto currentUser,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        if (role is not (UserRole.Teacher or UserRole.Student))
        {
            throw new InvalidOperationException("Chỉ được phân quyền Teacher hoặc Student.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");

        if (user.Role == UserRole.Admin)
        {
            throw new InvalidOperationException("Không được thay đổi quyền Admin tại màn hình này.");
        }

        user.Role = role;
        user.AccountStatus = AccountStatus.Active;
        user.IsActive = true;
        user.ApprovedAt = DateTime.UtcNow;
        user.ApprovedByAdminId = currentUser.UserId;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return ToDto(user);
    }

    public async Task<AccountDto> ActivateAccountAsync(
        CurrentUserDto currentUser,
        int userId,
        CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");

        if (user.Role == UserRole.Admin)
        {
            throw new InvalidOperationException("Không được kích hoạt lại tài khoản Admin tại màn hình này.");
        }

        if (user.Role == UserRole.Pending)
        {
            user.AccountStatus = AccountStatus.Pending;
            user.IsActive = false;
        }
        else
        {
            user.AccountStatus = AccountStatus.Active;
            user.IsActive = true;
            user.ApprovedAt ??= DateTime.UtcNow;
            user.ApprovedByAdminId ??= currentUser.UserId;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        return ToDto(user);
    }

    public async Task<AccountDto> LockAccountAsync(
        CurrentUserDto currentUser,
        int userId,
        CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        if (currentUser.UserId == userId)
        {
            throw new InvalidOperationException("Không thể khóa chính tài khoản đang đăng nhập.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");

        if (user.Role == UserRole.Admin)
        {
            throw new InvalidOperationException("Không được khóa tài khoản Admin tại màn hình này.");
        }

        user.AccountStatus = AccountStatus.Locked;
        user.IsActive = false;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return ToDto(user);
    }

    private static AccountDto ToDto(User user)
    {
        return new AccountDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            RoleLabel = GetRoleLabel(user.Role),
            AccountStatus = user.AccountStatus,
            AccountStatusLabel = GetStatusLabel(user.AccountStatus),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            ApprovedAt = user.ApprovedAt,
            ApprovedByAdminId = user.ApprovedByAdminId,
            ApprovedByAdminName = user.ApprovedByAdmin?.FullName ?? string.Empty
        };
    }

    private static string GetRoleLabel(UserRole role) => role switch
    {
        UserRole.Admin => "Admin",
        UserRole.Teacher => "Teacher",
        UserRole.Student => "Student",
        UserRole.Pending => "Chưa phân quyền",
        _ => role.ToString()
    };

    private static string GetStatusLabel(AccountStatus status) => status switch
    {
        AccountStatus.Pending => "Chờ phân quyền",
        AccountStatus.Active => "Đang hoạt động",
        AccountStatus.Locked => "Đã khóa",
        _ => status.ToString()
    };
}
