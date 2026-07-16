using DAL.Entities;

namespace BLL.DTOs;

public class PaymentDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string UserFullName { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

    public int SubscriptionPackageId { get; set; }

    public string SubscriptionPackageName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public string StatusLabel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? ReviewedByAdminId { get; set; }

    public string ReviewedByAdminName { get; set; } = string.Empty;

    public string? Note { get; set; }

    public PaymentGateway Gateway { get; set; } = PaymentGateway.None;

    public string GatewayLabel { get; set; } = string.Empty;

    public string? GatewayQrUrl { get; set; }

    public string? GatewayPayUrl { get; set; }

    public string? GatewayDeeplink { get; set; }

    public string? GatewayOrderId { get; set; }

    public DateTime? ExpiresAt { get; set; }
}