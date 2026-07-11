using BLL.DTOs;

namespace BLL.Interfaces;

public interface ISubjectService
{
    Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default);
}
