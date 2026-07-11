using DAL.Entities;

namespace DAL.Interfaces;

public interface ITeacherSubjectRepository
{
    Task<IReadOnlyList<TeacherSubject>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int teacherId, int subjectId, CancellationToken cancellationToken = default);

    Task AddAsync(TeacherSubject teacherSubject, CancellationToken cancellationToken = default);
}
