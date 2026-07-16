namespace DAL.Entities;

public class UserSubscription
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int SubscriptionPackageId { get; set; }

    public int PaymentId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int RemainingTokens { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeactivatedAt { get; set; }

    public int? ScheduledDowngradePackageId { get; set; }

    public User? User { get; set; }

    public SubscriptionPackage? SubscriptionPackage { get; set; }

    public SubscriptionPackage? ScheduledDowngradePackage { get; set; }

    public Payment? Payment { get; set; }
}