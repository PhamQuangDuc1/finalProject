using BLL.Interfaces;
using finalProject.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

[Authorize(Roles = StudyMateRoles.Admin)]
public class ChunkConfigurationsController : Controller
{
    private readonly IChunkConfigurationService _chunkConfigurationService;

    public ChunkConfigurationsController(IChunkConfigurationService chunkConfigurationService)
    {
        _chunkConfigurationService = chunkConfigurationService;
    }

    public IActionResult Index()
    {
        return View();
    }
}
