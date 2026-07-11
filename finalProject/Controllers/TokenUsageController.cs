using BLL.Interfaces;
using finalProject.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

[Authorize(Roles = StudyMateRoles.Admin)]
public class TokenUsageController : Controller
{
    private readonly ITokenUsageStatisticsService _tokenUsageStatisticsService;

    public TokenUsageController(ITokenUsageStatisticsService tokenUsageStatisticsService)
    {
        _tokenUsageStatisticsService = tokenUsageStatisticsService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summaries = await _tokenUsageStatisticsService.GetMonthlySummariesAsync(cancellationToken);

        return View(summaries);
    }
}
