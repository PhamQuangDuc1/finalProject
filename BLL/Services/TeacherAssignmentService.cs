using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class TeacherAssignmentService : ITeacherAssignmentService
{
    private readonly ISubjectService _subjectService;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly IUserRepository _userRepository;

    public TeacherAssignmentService(
        ISubjectService subjectService,
        ITeacherSubjectRepository teacherSubjectRepository,
        IUserRepository userRepository)
    {
        _subjectService = subjectService;
        _teacherSubjectRepository = teacherSubjectRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<TeacherAssignmentDto>> GetAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        var assignments = await _teacherSubjectRepository.GetAllAsync(cancellationToken);

        return assignments.Select(assignment => new TeacherAssignmentDto
        {
            Id = assignment.Id,
            TeacherId = assignment.TeacherId,
            TeacherName = assignment.Teacher?.FullName ?? string.Empty,
            SubjectId = assignment.SubjectId,
            SubjectName = assignment.Subject?.Name ?? string.Empty,
            DepartmentName = assignment.Subject?.Department?.Name ?? string.Empty,
            AssignedAt = assignment.AssignedAt
        }).ToList();
    }

    public async Task<IReadOnlyList<TeacherOptionDto>> GetTeacherOptionsAsync(CancellationToken cancellationToken = default)
    {
        var teachers = await _userRepository.GetByRoleAsync(UserRole.Teacher, cancellationToken);

        return teachers.Select(teacher => new TeacherOptionDto
        {
            Id = teacher.Id,
            FullName = teacher.FullName
        }).ToList();
    }

    public async Task<IReadOnlyList<SubjectOptionDto>> GetSubjectOptionsAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectService.GetSubjectsAsync(cancellationToken);

        return subjects
            .Where(subject => subject.IsActive)
            .Select(subject => new SubjectOptionDto
            {
                Id = subject.Id,
                DisplayName = $"{subject.Code} - {subject.Name}"
            }).ToList();
    }

    public async Task<IReadOnlyList<SubjectOptionDto>> GetSubjectOptionsForTeacherAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        var assignments = await _teacherSubjectRepository.GetByTeacherAsync(teacherId, cancellationToken);

        return assignments
            .Where(assignment => assignment.Subject?.IsActive == true)
            .Select(assignment => new SubjectOptionDto
            {
                Id = assignment.SubjectId,
                DisplayName = $"{assignment.Subject!.Code} - {assignment.Subject.Name}"
            })
            .ToList();
    }

    public Task AssignTeacherToSubjectAsync(CurrentUserDto currentUser, int teacherId, int subjectId, CancellationToken cancellationToken = default)
    {
        return _subjectService.AssignTeacherToSubjectAsync(currentUser, teacherId, subjectId, cancellationToken);
    }
}
