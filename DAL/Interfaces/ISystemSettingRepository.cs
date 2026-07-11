using DAL.Entities;

namespace DAL.Interfaces;

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(SystemSetting setting, CancellationToken cancellationToken = default);
}
