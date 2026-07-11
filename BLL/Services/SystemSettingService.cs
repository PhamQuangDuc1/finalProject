using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class SystemSettingService : ISystemSettingService
{
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
            UpdatedAt = setting.UpdatedAt
        };
    }
}
