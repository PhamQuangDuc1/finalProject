using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;

    public DepartmentService(IDepartmentRepository departmentRepository, IUserRepository userRepository)
    {
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
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

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken);

        if (department is null)
        {
            return null;
        }

        return new DepartmentDto
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
        };
    }

    public async Task<IReadOnlyList<TeacherOptionDto>> GetTeacherOptionsAsync(CancellationToken cancellationToken = default)
    {
        var teachers = await _userRepository.GetByRoleAsync(UserRole.Teacher, cancellationToken);

        return teachers.Select(teacher => new TeacherOptionDto
        {
            Id = teacher.Id,
            FullName = teacher.FullName
        }).ToList();
    }

    public async Task<int> CreateDepartmentAsync(CurrentUserDto currentUser, CreateDepartmentDto department, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var entity = new Department
        {
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            CreatedAt = DateTime.UtcNow
        };

        await _departmentRepository.AddAsync(entity, cancellationToken);

        return entity.Id;
    }

    public async Task UpdateDepartmentAsync(CurrentUserDto currentUser, UpdateDepartmentDto department, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var entity = await _departmentRepository.GetByIdAsync(department.Id, cancellationToken)
            ?? throw new InvalidOperationException("Department was not found.");

        entity.Code = department.Code;
        entity.Name = department.Name;
        entity.Description = department.Description;
        entity.UpdatedAt = DateTime.UtcNow;

        await _departmentRepository.UpdateAsync(entity, cancellationToken);
    }

    public async Task AssignManagerAsync(CurrentUserDto currentUser, int departmentId, int teacherId, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var teacher = await _userRepository.GetByIdAsync(teacherId, cancellationToken)
            ?? throw new InvalidOperationException("Teacher was not found.");

        if (teacher.Role != UserRole.Teacher || !teacher.IsActive)
        {
            throw new InvalidOperationException("Department manager must be an active Teacher.");
        }

        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("Department was not found.");

        if (department.ManagerTeacherId is not null && department.ManagerTeacherId != teacherId)
        {
            throw new InvalidOperationException("This Department already has a manager.");
        }

        if (await _departmentRepository.IsTeacherManagingAnyDepartmentAsync(teacherId, departmentId, cancellationToken))
        {
            throw new InvalidOperationException("This Teacher is already managing another Department.");
        }

        department.ManagerTeacherId = teacherId;
        department.UpdatedAt = DateTime.UtcNow;

        await _departmentRepository.UpdateAsync(department, cancellationToken);
    }

    public async Task RemoveManagerAsync(CurrentUserDto currentUser, int departmentId, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("Department was not found.");

        department.ManagerTeacherId = null;
        department.UpdatedAt = DateTime.UtcNow;

        await _departmentRepository.UpdateAsync(department, cancellationToken);
    }
}
