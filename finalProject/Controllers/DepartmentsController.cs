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
public class DepartmentsController : Controller
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetDepartmentsAsync(cancellationToken);

        return View(departments);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetDepartmentByIdAsync(id, cancellationToken);

        return department is null ? NotFound() : View(department);
    }

    public IActionResult Create()
    {
        return View(new DepartmentFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _departmentService.CreateDepartmentAsync(GetCurrentUser(), new CreateDepartmentDto
        {
            Code = model.Code,
            Name = model.Name,
            Description = model.Description
        }, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetDepartmentByIdAsync(id, cancellationToken);

        if (department is null)
        {
            return NotFound();
        }

        return View(new DepartmentFormViewModel
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DepartmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _departmentService.UpdateDepartmentAsync(GetCurrentUser(), new UpdateDepartmentDto
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            Description = model.Description
        }, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> AssignManager(int id, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetDepartmentByIdAsync(id, cancellationToken);

        if (department is null)
        {
            return NotFound();
        }

        var model = new AssignManagerViewModel
        {
            DepartmentId = department.Id,
            DepartmentName = department.Name,
            CurrentManagerName = department.ManagerTeacherName,
            TeacherOptions = await GetTeacherOptionsAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignManager(AssignManagerViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.TeacherOptions = await GetTeacherOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            await _departmentService.AssignManagerAsync(GetCurrentUser(), model.DepartmentId, model.TeacherId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.TeacherOptions = await GetTeacherOptionsAsync(cancellationToken);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveManager(int id, CancellationToken cancellationToken)
    {
        await _departmentService.RemoveManagerAsync(GetCurrentUser(), id, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<SelectListItem>> GetTeacherOptionsAsync(CancellationToken cancellationToken)
    {
        var teachers = await _departmentService.GetTeacherOptionsAsync(cancellationToken);

        return teachers.Select(teacher => new SelectListItem(teacher.FullName, teacher.Id.ToString())).ToList();
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
