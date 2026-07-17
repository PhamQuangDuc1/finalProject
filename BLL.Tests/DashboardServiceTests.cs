using BLL.DTOs;
using BLL.Services;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_IncludesDocumentIds_ForStudentRecentUploads()
    {
        var documents = new[]
        {
            new Document
            {
                Id = 42,
                Title = "Lap trinh .NET",
                Status = DocumentStatus.Indexed,
                UploadedAt = new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc),
                Subject = new Subject { Name = "PRN222", IsActive = true },
                UploadedByTeacher = new User { FullName = "Teacher A" }
            }
        };
        var service = new DashboardService(
            new FakeDocumentRepository(documents),
            new FakeDepartmentRepository(),
            new FakeSubjectRepository(new[] { new Subject { Name = "PRN222", IsActive = true } }),
            new FakeUserRepository(),
            new FakeAiUsageRepository(),
            new FakeUserSubscriptionRepository());

        var dashboard = await service.GetDashboardAsync(new CurrentUserDto { UserId = 7, Role = UserRole.Student });

        Assert.Equal(42, dashboard.RecentUploads.Single().Id);
    }

    [Fact]
    public async Task GetDashboardAsync_IncludesRemainingTokens_ForStudentActiveSubscription()
    {
        var subscription = new UserSubscription
        {
            UserId = 7,
            RemainingTokens = 1234,
            IsActive = true,
            EndDate = DateTime.UtcNow.AddDays(7)
        };
        var service = new DashboardService(
            new FakeDocumentRepository(Array.Empty<Document>()),
            new FakeDepartmentRepository(),
            new FakeSubjectRepository(Array.Empty<Subject>()),
            new FakeUserRepository(),
            new FakeAiUsageRepository(),
            new FakeUserSubscriptionRepository(subscription));

        var dashboard = await service.GetDashboardAsync(new CurrentUserDto { UserId = 7, Role = UserRole.Student });

        var tokenMetric = Assert.Single(dashboard.Metrics, metric => metric.Label == "Token còn lại");
        Assert.Equal("1,234", tokenMetric.Value);
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        private readonly IReadOnlyList<Document> _documents;

        public FakeDocumentRepository(IReadOnlyList<Document> documents)
        {
            _documents = documents;
        }

        public Task<IReadOnlyList<Document>> GetAllForAdminAsync(CancellationToken cancellationToken = default) => Task.FromResult(_documents);

        public Task<IReadOnlyList<Document>> GetByTeacherAsync(int teacherId, CancellationToken cancellationToken = default) => Task.FromResult(_documents);

        public Task<IReadOnlyList<Document>> GetBySubjectIdsAsync(IReadOnlyCollection<int> subjectIds, CancellationToken cancellationToken = default) => Task.FromResult(_documents);

        public Task<IReadOnlyList<Document>> GetIndexedActiveAsync(CancellationToken cancellationToken = default) => Task.FromResult(_documents);

        public Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(_documents.FirstOrDefault(document => document.Id == id));

        public Task AddAsync(Document document, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task UpdateAsync(Document document, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task ReplaceChunksInTransactionAsync(Document document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task UpdateContentInTransactionAsync(
            Document document,
            DocumentVersion? previousVersion,
            IReadOnlyList<DocumentChunk> chunks,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Department>>(Array.Empty<Department>());

        public Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> HasManagerAsync(int departmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> IsTeacherManagingAnyDepartmentAsync(int teacherId, int? excludedDepartmentId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task AddAsync(Department department, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task UpdateAsync(Department department, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeSubjectRepository : ISubjectRepository
    {
        private readonly IReadOnlyList<Subject> _subjects;

        public FakeSubjectRepository(IReadOnlyList<Subject> subjects)
        {
            _subjects = subjects;
        }

        public Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(_subjects);

        public Task<Subject?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task AddAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<User?> ValidateUserAsync(string username, string passwordHash, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());

        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeAiUsageRepository : IAiUsageRepository
    {
        public Task AddAsync(AiUsageLog usageLog, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<AiUsageLog>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AiUsageLog>>(Array.Empty<AiUsageLog>());
    }

    private sealed class FakeUserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly UserSubscription? _subscription;

        public FakeUserSubscriptionRepository(UserSubscription? subscription = null)
        {
            _subscription = subscription;
        }

        public Task<IReadOnlyList<UserSubscription>> GetByUserAsync(int userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<UserSubscription?> GetActiveByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_subscription?.UserId == userId ? _subscription : null);
        }

        public Task<UserSubscription?> GetLatestActiveByUserAsync(int userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<UserSubscription?> GetByIdWithPackageAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<UserSubscription>> GetAllActiveWithUsersAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> HasActiveSubscriptionAsync(int userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public void Add(UserSubscription subscription) => throw new NotImplementedException();

        public Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task UpdateAsync(UserSubscription subscription, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task DeactivateExpiredAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> ProcessScheduledDowngradesAsync(DateTime now, CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
