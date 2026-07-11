using BLL.DTOs;

namespace BLL.Interfaces;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    Task<DepartmentDto?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherOptionDto>> GetTeacherOptionsAsync(CancellationToken cancellationToken = default);

    Task<int> CreateDepartmentAsync(CurrentUserDto currentUser, CreateDepartmentDto department, CancellationToken cancellationToken = default);

    Task UpdateDepartmentAsync(CurrentUserDto currentUser, UpdateDepartmentDto department, CancellationToken cancellationToken = default);

    Task AssignManagerAsync(CurrentUserDto currentUser, int departmentId, int teacherId, CancellationToken cancellationToken = default);

    Task RemoveManagerAsync(CurrentUserDto currentUser, int departmentId, CancellationToken cancellationToken = default);
}
