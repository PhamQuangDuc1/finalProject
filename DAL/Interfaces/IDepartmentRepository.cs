using DAL.Entities;

namespace DAL.Interfaces;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> HasManagerAsync(int departmentId, CancellationToken cancellationToken = default);

    Task<bool> IsTeacherManagingAnyDepartmentAsync(int teacherId, int? excludedDepartmentId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Department department, CancellationToken cancellationToken = default);

    Task UpdateAsync(Department department, CancellationToken cancellationToken = default);
}
