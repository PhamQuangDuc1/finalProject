namespace BLL.Interfaces;

using BLL.DTOs;

public interface IChunkConfigurationService
{
    Task<SystemSettingDto> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(CurrentUserDto currentUser, UpdateSystemSettingDto setting, CancellationToken cancellationToken = default);
}
