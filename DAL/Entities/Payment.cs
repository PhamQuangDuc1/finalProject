namespace DAL.Entities;

public class Payment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int SubscriptionPackageId { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public int? ReviewedByAdminId { get; set; }

    public string? Note { get; set; }

    public PaymentGateway Gateway { get; set; } = PaymentGateway.None;

    public string? GatewayTransactionId { get; set; }

    public string? GatewayOrderId { get; set; }

    public string? GatewayQrUrl { get; set; }

    public string? GatewayPayUrl { get; set; }

    public string? GatewayDeeplink { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public User? User { get; set; }

    public SubscriptionPackage? SubscriptionPackage { get; set; }

    public User? ReviewedByAdmin { get; set; }

    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}