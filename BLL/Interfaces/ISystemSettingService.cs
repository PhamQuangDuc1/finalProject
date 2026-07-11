using BLL.DTOs;

namespace BLL.Interfaces;

public interface ISystemSettingService
{
    Task<SystemSettingDto> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(CurrentUserDto currentUser, UpdateSystemSettingDto setting, CancellationToken cancellationToken = default);
}
