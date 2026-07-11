using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly IUserRepository _userRepository;

    public SubjectService(
        ISubjectRepository subjectRepository,
        IDepartmentRepository departmentRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        IUserRepository userRepository)
    {
        _subjectRepository = subjectRepository;
        _departmentRepository = departmentRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);

        return subjects.Select(ToDto).ToList();
    }

    public async Task<SubjectDto?> GetSubjectByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var subject = await _subjectRepository.GetByIdAsync(id, cancellationToken);

        return subject is null ? null : ToDto(subject);
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentOptionsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);

        return departments.Select(department => new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            ManagerTeacherId = department.ManagerTeacherId,
            ManagerTeacherName = department.ManagerTeacher?.FullName ?? string.Empty,
            NumberOfSubjects = department.Subjects.Count,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        }).ToList();
    }

    public async Task<int> CreateSubjectAsync(CurrentUserDto currentUser, CreateSubjectDto subject, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        _ = await _departmentRepository.GetByIdAsync(subject.DepartmentId, cancellationToken)
            ?? throw new InvalidOperationException("Department was not found.");

        var entity = new Subject
        {
            DepartmentId = subject.DepartmentId,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _subjectRepository.AddAsync(entity, cancellationToken);

        return entity.Id;
    }

    public async Task UpdateSubjectAsync(CurrentUserDto currentUser, UpdateSubjectDto subject, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var entity = await _subjectRepository.GetByIdAsync(subject.Id, cancellationToken)
            ?? throw new InvalidOperationException("Subject was not found.");

        _ = await _departmentRepository.GetByIdAsync(subject.DepartmentId, cancellationToken)
            ?? throw new InvalidOperationException("Department was not found.");

        entity.DepartmentId = subject.DepartmentId;
        entity.Code = subject.Code;
        entity.Name = subject.Name;
        entity.Description = subject.Description;
        entity.IsActive = subject.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _subjectRepository.UpdateAsync(entity, cancellationToken);
    }

    public async Task AssignTeacherToSubjectAsync(CurrentUserDto currentUser, int teacherId, int subjectId, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var teacher = await _userRepository.GetByIdAsync(teacherId, cancellationToken)
            ?? throw new InvalidOperationException("Teacher was not found.");

        if (teacher.Role != UserRole.Teacher || !teacher.IsActive)
        {
            throw new InvalidOperationException("Only an active Teacher can be assigned to a Subject.");
        }

        _ = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken)
            ?? throw new InvalidOperationException("Subject was not found.");

        if (await _teacherSubjectRepository.ExistsAsync(teacherId, subjectId, cancellationToken))
        {
            return;
        }

        await _teacherSubjectRepository.AddAsync(new TeacherSubject
        {
            TeacherId = teacherId,
            SubjectId = subjectId,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task ActivateAsync(CurrentUserDto currentUser, int subjectId, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken)
            ?? throw new InvalidOperationException("Subject was not found.");

        subject.IsActive = true;
        subject.UpdatedAt = DateTime.UtcNow;

        await _subjectRepository.UpdateAsync(subject, cancellationToken);
    }

    public async Task DeactivateAsync(CurrentUserDto currentUser, int subjectId, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken)
            ?? throw new InvalidOperationException("Subject was not found.");

        subject.IsActive = false;
        subject.UpdatedAt = DateTime.UtcNow;

        await _subjectRepository.UpdateAsync(subject, cancellationToken);
    }

    private static SubjectDto ToDto(Subject subject)
    {
        return new SubjectDto
        {
            Id = subject.Id,
            DepartmentId = subject.DepartmentId,
            DepartmentName = subject.Department?.Name ?? string.Empty,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description,
            IsActive = subject.IsActive,
            AssignedTeacherNames = subject.TeacherSubjects
                .Where(assignment => assignment.Teacher is not null)
                .Select(assignment => assignment.Teacher!.FullName)
                .ToList()
        };
    }
}
