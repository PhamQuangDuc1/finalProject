using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly AppDbContext _dbContext;

    public SubjectRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Subjects
            .Include(subject => subject.Department)
            .Include(subject => subject.TeacherSubjects)
                .ThenInclude(teacherSubject => teacherSubject.Teacher)
            .OrderBy(subject => subject.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Subject?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Subjects
            .Include(subject => subject.Department)
            .Include(subject => subject.TeacherSubjects)
                .ThenInclude(teacherSubject => teacherSubject.Teacher)
            .FirstOrDefaultAsync(subject => subject.Id == id, cancellationToken);
    }

    public async Task AddAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        _dbContext.Subjects.Add(subject);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        _dbContext.Subjects.Update(subject);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
