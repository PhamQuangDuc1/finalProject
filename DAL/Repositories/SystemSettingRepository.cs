using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly AppDbContext _dbContext;

    public SystemSettingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SystemSetting?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SystemSettings
            .Include(setting => setting.UpdatedByAdmin)
            .OrderByDescending(setting => setting.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(SystemSetting setting, CancellationToken cancellationToken = default)
    {
        _dbContext.SystemSettings.Update(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
