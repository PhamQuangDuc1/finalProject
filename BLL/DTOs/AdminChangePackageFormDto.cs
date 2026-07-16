namespace BLL.DTOs;

public class AdminChangePackageFormDto
{
    public AdminUserSubscriptionSummaryDto CurrentSubscription { get; set; } = new();

    public IReadOnlyList<SubscriptionPackageDto> AvailablePackages { get; set; } = Array.Empty<SubscriptionPackageDto>();
}