using BLL.Interfaces;
using BLL.Services;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_ReturnsUser_WhenPasswordIsCorrect()
    {
        var repository = new FakeUserRepository(new User
        {
            Id = 2,
            Username = "teacherA",
            FullName = "Teacher A",
            Email = "teacherA@studymate.local",
            Role = UserRole.Teacher,
            AccountStatus = AccountStatus.Active,
            IsActive = true,
            PasswordHash = PasswordHashService.HashPassword("123456")
        });
        IAuthenticationService service = new AuthenticationService(repository);

        var result = await service.AuthenticateAsync("teacherA", "123456");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);
        Assert.Equal(2, result.User.Id);
        Assert.Equal("teacherA", result.User.Username);
        Assert.Equal(UserRole.Teacher, result.User.Role);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenPasswordIsWrong()
    {
        var repository = new FakeUserRepository(new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@studymate.local",
            FullName = "Admin",
            Role = UserRole.Admin,
            AccountStatus = AccountStatus.Active,
            IsActive = true,
            PasswordHash = PasswordHashService.HashPassword("123456")
        });
        IAuthenticationService service = new AuthenticationService(repository);

        var result = await service.AuthenticateAsync("admin", "wrong-password");

        Assert.False(result.Succeeded);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _user;

        public FakeUserRepository(User user)
        {
            _user = user;
        }

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_user.Id == id ? _user : null);
        }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(string.Equals(_user.Username, username, StringComparison.OrdinalIgnoreCase) ? _user : null);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(string.Equals(_user.Email, email, StringComparison.OrdinalIgnoreCase) ? _user : null);
        }

        public async Task<User?> ValidateUserAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
        {
            var user = await GetByUsernameAsync(username, cancellationToken);

            return user?.PasswordHash == passwordHash ? user : null;
        }

        public Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<User> users = _user.Role == role ? new[] { _user } : Array.Empty<User>();

            return Task.FromResult(users);
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(new[] { _user });
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
