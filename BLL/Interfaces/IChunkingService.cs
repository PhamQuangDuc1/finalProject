using BLL.DTOs;

namespace BLL.Interfaces;

public interface IChunkingService
{
    Task<SystemSettingDto> GetCurrentChunkSettingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DAL.Entities.DocumentChunk>> CreateChunksAsync(
        int documentId,
        string text,
        CancellationToken cancellationToken = default);
}
