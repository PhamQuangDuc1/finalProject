using BLL.DTOs;

namespace BLL.Interfaces;

public interface IChunkingService
{
    Task<SystemSettingDto> GetCurrentChunkSettingAsync(CancellationToken cancellationToken = default);
}
