namespace BLL.DTOs;

public class UserSubscriptionDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int SubscriptionPackageId { get; set; }

    public string SubscriptionPackageName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int RemainingTokens { get; set; }

    public int MaxTokens { get; set; }

    public bool IsActive { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public int DaysRemaining => IsActive && EndDate > DateTime.UtcNow
        ? Math.Max(0, (int)Math.Ceiling((EndDate - DateTime.UtcNow).TotalDays))
        : 0;
}