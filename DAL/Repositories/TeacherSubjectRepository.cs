using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class TeacherSubjectRepository : ITeacherSubjectRepository
{
    private readonly AppDbContext _dbContext;

    public TeacherSubjectRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TeacherSubject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeacherSubjects
            .Include(assignment => assignment.Teacher)
            .Include(assignment => assignment.Subject)
                .ThenInclude(subject => subject!.Department)
            .OrderBy(assignment => assignment.Subject!.Name)
            .ThenBy(assignment => assignment.Teacher!.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int teacherId, int subjectId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TeacherSubjects.AnyAsync(
            assignment => assignment.TeacherId == teacherId && assignment.SubjectId == subjectId,
            cancellationToken);
    }

    public async Task AddAsync(TeacherSubject teacherSubject, CancellationToken cancellationToken = default)
    {
        _dbContext.TeacherSubjects.Add(teacherSubject);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
