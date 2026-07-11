using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

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
