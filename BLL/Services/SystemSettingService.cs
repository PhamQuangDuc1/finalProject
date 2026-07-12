using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class SystemSettingService : ISystemSettingService, IChunkConfigurationService
{
    private const int MaximumTopK = 50;

    private readonly ISystemSettingRepository _systemSettingRepository;

    public SystemSettingService(ISystemSettingRepository systemSettingRepository)
    {
        _systemSettingRepository = systemSettingRepository;
    }

    public async Task<SystemSettingDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _systemSettingRepository.GetCurrentAsync(cancellationToken)
            ?? throw new InvalidOperationException("System setting was not found.");

        return ToDto(setting);
    }

    public async Task UpdateAsync(CurrentUserDto currentUser, UpdateSystemSettingDto setting, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);
        ValidateSetting(setting);

        var entity = await _systemSettingRepository.GetCurrentAsync(cancellationToken)
            ?? throw new InvalidOperationException("System setting was not found.");

        entity.ChunkStrategy = setting.ChunkStrategy;
        entity.ChunkSize = setting.ChunkSize;
        entity.ChunkOverlap = setting.ChunkOverlap;
        entity.TopK = setting.TopK;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByAdminId = currentUser.UserId;

        await _systemSettingRepository.UpdateAsync(entity, cancellationToken);
    }

    private static SystemSettingDto ToDto(SystemSetting setting)
    {
        return new SystemSettingDto
        {
            Id = setting.Id,
            ChunkStrategy = setting.ChunkStrategy,
            ChunkSize = setting.ChunkSize,
            ChunkOverlap = setting.ChunkOverlap,
            TopK = setting.TopK,
            UpdatedAt = setting.UpdatedAt,
            UpdatedByAdminId = setting.UpdatedByAdminId,
            UpdatedByAdminName = setting.UpdatedByAdmin?.FullName ?? string.Empty
        };
    }

    private static void ValidateSetting(UpdateSystemSettingDto setting)
    {
        if (!Enum.IsDefined(setting.ChunkStrategy))
        {
            throw new InvalidOperationException("Chunk strategy is not valid.");
        }

        if (setting.ChunkSize <= 0)
        {
            throw new InvalidOperationException("Chunk Size must be greater than 0.");
        }

        if (setting.ChunkOverlap < 0)
        {
            throw new InvalidOperationException("Chunk Overlap must be greater than or equal to 0.");
        }

        if (setting.ChunkOverlap >= setting.ChunkSize)
        {
            throw new InvalidOperationException("Chunk Overlap must be smaller than Chunk Size.");
        }

        if (setting.TopK <= 0 || setting.TopK > MaximumTopK)
        {
            throw new InvalidOperationException($"Top-K must be between 1 and {MaximumTopK}.");
        }
    }
}
