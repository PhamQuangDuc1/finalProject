using BLL.DTOs;

namespace BLL.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);
}
