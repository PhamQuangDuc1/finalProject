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
public class SubjectsController : Controller
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var subjects = await _subjectService.GetSubjectsAsync(cancellationToken);

        return View(subjects);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(id, cancellationToken);

        return subject is null ? NotFound() : View(subject);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new SubjectFormViewModel
        {
            DepartmentOptions = await GetDepartmentOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubjectFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.DepartmentOptions = await GetDepartmentOptionsAsync(cancellationToken);
            return View(model);
        }

        await _subjectService.CreateSubjectAsync(GetCurrentUser(), new CreateSubjectDto
        {
            DepartmentId = model.DepartmentId,
            Code = model.Code,
            Name = model.Name,
            Description = model.Description
        }, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(id, cancellationToken);

        if (subject is null)
        {
            return NotFound();
        }

        return View(new SubjectFormViewModel
        {
            Id = subject.Id,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description,
            DepartmentId = subject.DepartmentId,
            IsActive = subject.IsActive,
            DepartmentOptions = await GetDepartmentOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SubjectFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.DepartmentOptions = await GetDepartmentOptionsAsync(cancellationToken);
            return View(model);
        }

        await _subjectService.UpdateSubjectAsync(GetCurrentUser(), new UpdateSubjectDto
        {
            Id = model.Id,
            DepartmentId = model.DepartmentId,
            Code = model.Code,
            Name = model.Name,
            Description = model.Description,
            IsActive = model.IsActive
        }, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        await _subjectService.ActivateAsync(GetCurrentUser(), id, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _subjectService.DeactivateAsync(GetCurrentUser(), id, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<SelectListItem>> GetDepartmentOptionsAsync(CancellationToken cancellationToken)
    {
        var departments = await _subjectService.GetDepartmentOptionsAsync(cancellationToken);

        return departments.Select(department => new SelectListItem($"{department.Code} - {department.Name}", department.Id.ToString())).ToList();
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
