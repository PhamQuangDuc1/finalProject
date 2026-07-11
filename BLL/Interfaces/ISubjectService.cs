using BLL.DTOs;

namespace BLL.Interfaces;

public interface ISubjectService
{
    Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default);

    Task<SubjectDto?> GetSubjectByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDto>> GetDepartmentOptionsAsync(CancellationToken cancellationToken = default);

    Task<int> CreateSubjectAsync(CurrentUserDto currentUser, CreateSubjectDto subject, CancellationToken cancellationToken = default);

    Task UpdateSubjectAsync(CurrentUserDto currentUser, UpdateSubjectDto subject, CancellationToken cancellationToken = default);

    Task AssignTeacherToSubjectAsync(CurrentUserDto currentUser, int teacherId, int subjectId, CancellationToken cancellationToken = default);

    Task ActivateAsync(CurrentUserDto currentUser, int subjectId, CancellationToken cancellationToken = default);

    Task DeactivateAsync(CurrentUserDto currentUser, int subjectId, CancellationToken cancellationToken = default);
}
