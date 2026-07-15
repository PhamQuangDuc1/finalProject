using BLL.DTOs;
using BLL.Interfaces;
using BLL.Services;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Tests;

public class TeacherAssignmentServiceTests
{
    [Fact]
    public async Task RemoveTeacherFromSubjectAsync_RemovesOnlyTeacherSubject_WhenAdminRequests()
    {
        var teacher = new User { Id = 2, FullName = "Teacher A", Role = UserRole.Teacher, IsActive = true };
        var subject = new Subject { Id = 10, Name = "Lập trình .NET", IsActive = true };
        var assignment = new TeacherSubject
        {
            Id = 7,
            TeacherId = teacher.Id,
            Teacher = teacher,
            SubjectId = subject.Id,
            Subject = subject
        };
        var repository = new FakeTeacherSubjectRepository(new[] { assignment });
        var service = CreateService(repository);

        await service.RemoveTeacherFromSubjectAsync(new CurrentUserDto { UserId = 1, Role = UserRole.Admin }, assignment.Id);

        Assert.Empty(repository.Assignments);
        Assert.Same(teacher, assignment.Teacher);
        Assert.Same(subject, assignment.Subject);
    }

    [Fact]
    public async Task RemoveTeacherFromSubjectAsync_Throws_WhenCurrentUserIsNotAdmin()
    {
        var assignment = new TeacherSubject { Id = 7, TeacherId = 2, SubjectId = 10 };
        var repository = new FakeTeacherSubjectRepository(new[] { assignment });
        var service = CreateService(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RemoveTeacherFromSubjectAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            assignment.Id));

        Assert.Single(repository.Assignments);
    }

    [Fact]
    public async Task RemoveTeacherFromSubjectAsync_Throws_WhenAssignmentDoesNotExist()
    {
        var repository = new FakeTeacherSubjectRepository(Array.Empty<TeacherSubject>());
        var service = CreateService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveTeacherFromSubjectAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
            999));
    }

    private static TeacherAssignmentService CreateService(FakeTeacherSubjectRepository repository)
    {
        return new TeacherAssignmentService(new FakeSubjectService(), repository, new FakeUserRepository());
    }

    private sealed class FakeTeacherSubjectRepository : ITeacherSubjectRepository
    {
        public FakeTeacherSubjectRepository(IEnumerable<TeacherSubject> assignments)
        {
            Assignments = assignments.ToList();
        }

        public List<TeacherSubject> Assignments { get; }

        public Task<IReadOnlyList<TeacherSubject>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TeacherSubject>>(Assignments);
        }

        public Task<TeacherSubject?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Assignments.FirstOrDefault(assignment => assignment.Id == id));
        }

        public Task<IReadOnlyList<TeacherSubject>> GetByTeacherAsync(int teacherId, CancellationToken cancellationToken = default)
        {
            var result = Assignments.Where(assignment => assignment.TeacherId == teacherId).ToList();
            return Task.FromResult<IReadOnlyList<TeacherSubject>>(result);
        }

        public Task<bool> ExistsAsync(int teacherId, int subjectId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Assignments.Any(assignment => assignment.TeacherId == teacherId && assignment.SubjectId == subjectId));
        }

        public Task AddAsync(TeacherSubject teacherSubject, CancellationToken cancellationToken = default)
        {
            Assignments.Add(teacherSubject);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TeacherSubject teacherSubject, CancellationToken cancellationToken = default)
        {
            Assignments.Remove(teacherSubject);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSubjectService : ISubjectService
    {
        public Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SubjectDto>>(Array.Empty<SubjectDto>());
        }

        public Task<SubjectDto?> GetSubjectByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SubjectDto?>(null);
        }

        public Task<IReadOnlyList<DepartmentDto>> GetDepartmentOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DepartmentDto>>(Array.Empty<DepartmentDto>());
        }

        public Task<int> CreateSubjectAsync(CurrentUserDto currentUser, CreateSubjectDto subject, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateSubjectAsync(CurrentUserDto currentUser, UpdateSubjectDto subject, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task AssignTeacherToSubjectAsync(CurrentUserDto currentUser, int teacherId, int subjectId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ActivateAsync(CurrentUserDto currentUser, int subjectId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeactivateAsync(CurrentUserDto currentUser, int subjectId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> ValidateUserAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());
        }
    }
}
