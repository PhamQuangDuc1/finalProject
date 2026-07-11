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
            Role = UserRole.Teacher,
            IsActive = true,
            PasswordHash = PasswordHashService.HashPassword("123456")
        });
        IAuthenticationService service = new AuthenticationService(repository);

        var result = await service.AuthenticateAsync("teacherA", "123456");

        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("teacherA", result.Username);
        Assert.Equal(UserRole.Teacher, result.Role);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenPasswordIsWrong()
    {
        var repository = new FakeUserRepository(new User
        {
            Id = 1,
            Username = "admin",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true,
            PasswordHash = PasswordHashService.HashPassword("123456")
        });
        IAuthenticationService service = new AuthenticationService(repository);

        var result = await service.AuthenticateAsync("admin", "wrong-password");

        Assert.Null(result);
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
    }
}
