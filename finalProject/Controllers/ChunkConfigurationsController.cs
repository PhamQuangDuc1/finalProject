using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

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
