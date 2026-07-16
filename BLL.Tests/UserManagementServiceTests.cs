using BLL.DTOs;
using BLL.Services;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Tests;

public class UserManagementServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesPendingInactiveUser_WhenDataIsValid()
    {
        var repository = new FakeUserRepository();
        var service = new UserManagementService(repository);

        var result = await service.RegisterAsync(new RegisterDto
        {
            FullName = "Nguyễn Văn Mới",
            Email = "new.user@example.com",
            Username = "newuser",
            Password = "123456"
        });

        Assert.Equal(UserRole.Pending, result.Role);
        Assert.Equal(AccountStatus.Pending, result.AccountStatus);
        Assert.False(result.IsActive);

        var saved = Assert.Single(repository.Users);
        Assert.Equal("new.user@example.com", saved.Email);
        Assert.Equal(UserRole.Pending, saved.Role);
        Assert.Equal(AccountStatus.Pending, saved.AccountStatus);
        Assert.False(saved.IsActive);
        Assert.NotEqual("123456", saved.PasswordHash);
    }

    [Fact]
    public async Task AssignRoleAsync_Throws_WhenCurrentUserIsNotAdmin()
    {
        var repository = new FakeUserRepository(new User
        {
            Id = 10,
            Username = "pending",
            Email = "pending@example.com",
            FullName = "Pending User",
            Role = UserRole.Pending,
            AccountStatus = AccountStatus.Pending,
            IsActive = false
        });
        var service = new UserManagementService(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AssignRoleAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            10,
            UserRole.Student));
    }

    [Fact]
    public async Task AssignRoleAsync_Throws_WhenUserDoesNotExist()
    {
        var service = new UserManagementService(new FakeUserRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AssignRoleAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
            999,
            UserRole.Student));
    }

    [Fact]
    public async Task AssignRoleAsync_ActivatesUser_WhenAdminAssignsTeacher()
    {
        var repository = new FakeUserRepository(new User
        {
            Id = 10,
            Username = "pending",
            Email = "pending@example.com",
            FullName = "Pending User",
            Role = UserRole.Pending,
            AccountStatus = AccountStatus.Pending,
            IsActive = false
        });
        var service = new UserManagementService(repository);

        var result = await service.AssignRoleAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
            10,
            UserRole.Teacher);

        Assert.Equal(UserRole.Teacher, result.Role);
        Assert.Equal(AccountStatus.Active, result.AccountStatus);
        Assert.True(result.IsActive);
        Assert.Equal(1, repository.Users.Single().ApprovedByAdminId);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public FakeUserRepository(params User[] users)
        {
            Users = users.ToList();
        }

        public List<User> Users { get; }

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.FirstOrDefault(user =>
                string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.FirstOrDefault(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<User?> ValidateUserAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
        {
            var user = await GetByUsernameAsync(username, cancellationToken);

            return user?.PasswordHash == passwordHash ? user : null;
        }

        public Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Users.Where(user => user.Role == role).ToList());
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Users);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            user.Id = Users.Count == 0 ? 1 : Users.Max(existing => existing.Id) + 1;
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
