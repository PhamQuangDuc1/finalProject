using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

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
}
