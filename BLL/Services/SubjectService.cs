using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;

namespace BLL.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;

    public SubjectService(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public async Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);

        return subjects.Select(subject => new SubjectDto
        {
            Id = subject.Id,
            DepartmentId = subject.DepartmentId,
            DepartmentName = subject.Department?.Name ?? string.Empty,
            Code = subject.Code,
            Name = subject.Name
        }).ToList();
    }
}
