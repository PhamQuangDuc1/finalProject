using BLL.DTOs;
using BLL.Interfaces;

namespace BLL.Services;

public class ChunkingService : IChunkingService, IChunkConfigurationService
{
    private readonly ISystemSettingService _systemSettingService;

    public ChunkingService(ISystemSettingService systemSettingService)
    {
        _systemSettingService = systemSettingService;
    }

    public Task<SystemSettingDto> GetCurrentChunkSettingAsync(CancellationToken cancellationToken = default)
    {
        return _systemSettingService.GetCurrentAsync(cancellationToken);
    }
}
