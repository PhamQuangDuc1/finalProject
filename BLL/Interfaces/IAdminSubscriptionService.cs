using BLL.DTOs;

namespace BLL.Interfaces;

public interface IAdminSubscriptionService
{
    Task<IReadOnlyList<AdminUserSubscriptionSummaryDto>> GetAllActiveSubscriptionsAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task<AdminChangePackageFormDto?> GetChangePackageFormAsync(CurrentUserDto currentUser, int subscriptionId, CancellationToken cancellationToken = default);

    Task<AdminUserSubscriptionSummaryDto?> CancelSubscriptionAsync(CurrentUserDto currentUser, int subscriptionId, string? reason, CancellationToken cancellationToken = default);

    Task<AdminUserSubscriptionSummaryDto?> ChangePackageAsync(CurrentUserDto currentUser, SubscriptionChangeRequestDto request, CancellationToken cancellationToken = default);
}