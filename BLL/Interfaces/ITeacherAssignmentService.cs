using BLL.DTOs;

namespace BLL.Interfaces;

public interface ITeacherAssignmentService
{
    Task<IReadOnlyList<TeacherAssignmentDto>> GetAssignmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherOptionDto>> GetTeacherOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectOptionDto>> GetSubjectOptionsAsync(CancellationToken cancellationToken = default);

    Task AssignTeacherToSubjectAsync(CurrentUserDto currentUser, int teacherId, int subjectId, CancellationToken cancellationToken = default);
}
