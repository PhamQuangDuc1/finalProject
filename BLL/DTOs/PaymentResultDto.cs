using DAL.Entities;

namespace BLL.DTOs;

public class PaymentResultDto
{
    public int PaymentId { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public string StatusLabel { get; set; } = string.Empty;

    public PaymentGateway Gateway { get; set; }

    public string GatewayLabel { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public string? PayUrl { get; set; }

    public string? QrCodeUrl { get; set; }

    public string? Deeplink { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}
