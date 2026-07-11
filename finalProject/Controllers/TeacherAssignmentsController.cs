using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using finalProject.Authorization;
using finalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.Controllers;

[Authorize(Roles = StudyMateRoles.Admin)]
public class TeacherAssignmentsController : Controller
{
    private readonly ITeacherAssignmentService _teacherAssignmentService;

    public TeacherAssignmentsController(ITeacherAssignmentService teacherAssignmentService)
    {
        _teacherAssignmentService = teacherAssignmentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new TeacherAssignmentsIndexViewModel
        {
            Form = new TeacherAssignmentFormViewModel
            {
                TeacherOptions = await GetTeacherOptionsAsync(cancellationToken),
                SubjectOptions = await GetSubjectOptionsAsync(cancellationToken)
            },
            Assignments = await _teacherAssignmentService.GetAssignmentsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign([Bind(Prefix = "Form")] TeacherAssignmentFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.TeacherOptions = await GetTeacherOptionsAsync(cancellationToken);
            form.SubjectOptions = await GetSubjectOptionsAsync(cancellationToken);

            return View(nameof(Index), new TeacherAssignmentsIndexViewModel
            {
                Form = form,
                Assignments = await _teacherAssignmentService.GetAssignmentsAsync(cancellationToken)
            });
        }

        await _teacherAssignmentService.AssignTeacherToSubjectAsync(GetCurrentUser(), form.TeacherId, form.SubjectId, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<SelectListItem>> GetTeacherOptionsAsync(CancellationToken cancellationToken)
    {
        var teachers = await _teacherAssignmentService.GetTeacherOptionsAsync(cancellationToken);

        return teachers.Select(teacher => new SelectListItem(teacher.FullName, teacher.Id.ToString())).ToList();
    }

    private async Task<IReadOnlyList<SelectListItem>> GetSubjectOptionsAsync(CancellationToken cancellationToken)
    {
        var subjects = await _teacherAssignmentService.GetSubjectOptionsAsync(cancellationToken);

        return subjects.Select(subject => new SelectListItem(subject.DisplayName, subject.Id.ToString())).ToList();
    }

    private CurrentUserDto GetCurrentUser()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return new CurrentUserDto
        {
            UserId = int.TryParse(userIdValue, out var userId) ? userId : 0,
            Role = UserRole.Admin
        };
    }
}
