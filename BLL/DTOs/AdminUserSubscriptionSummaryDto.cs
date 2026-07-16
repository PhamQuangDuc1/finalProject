namespace BLL.DTOs;

public class AdminUserSubscriptionSummaryDto
{
    public int SubscriptionId { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string SubscriptionPackageName { get; set; } = string.Empty;

    public int SubscriptionPackageId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int RemainingTokens { get; set; }

    public int MaxTokens { get; set; }

    public int? ScheduledDowngradePackageId { get; set; }

    public string? ScheduledDowngradePackageName { get; set; }

    public int DaysRemaining => EndDate > DateTime.UtcNow
        ? Math.Max(0, (int)Math.Ceiling((EndDate - DateTime.UtcNow).TotalDays))
        : 0;
}