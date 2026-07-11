using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _dbContext;

    public DepartmentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Departments
            .Include(department => department.ManagerTeacher)
            .Include(department => department.Subjects)
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Departments
            .Include(department => department.ManagerTeacher)
            .Include(department => department.Subjects)
            .FirstOrDefaultAsync(department => department.Id == id, cancellationToken);
    }

    public Task<bool> HasManagerAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Departments.AnyAsync(
            department => department.Id == departmentId && department.ManagerTeacherId != null,
            cancellationToken);
    }

    public Task<bool> IsTeacherManagingAnyDepartmentAsync(int teacherId, int? excludedDepartmentId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.Departments.AnyAsync(
            department => department.ManagerTeacherId == teacherId
                && (!excludedDepartmentId.HasValue || department.Id != excludedDepartmentId.Value),
            cancellationToken);
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken = default)
    {
        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Department department, CancellationToken cancellationToken = default)
    {
        _dbContext.Departments.Update(department);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
