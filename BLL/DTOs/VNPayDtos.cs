namespace BLL.DTOs;

public class VNPayCreatePaymentRequest
{
    public int PaymentId { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public long Amount { get; set; }

    public string OrderInfo { get; set; } = string.Empty;

    public string IpAddress { get; set; } = "127.0.0.1";

    public string? Locale { get; set; }

    public string? BankCode { get; set; }
}

public class VNPayCreatePaymentResult
{
    public int PaymentId { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string PayUrl { get; set; } = string.Empty;

    public string? QrCodeUrl { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsSuccess => !string.IsNullOrWhiteSpace(PayUrl);
}

public class VNPayReturnPayload
{
    public string TmnCode { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string TransactionNo { get; set; } = string.Empty;

    public string ResponseCode { get; set; } = string.Empty;

    public string TransactionStatus { get; set; } = string.Empty;

    public long Amount { get; set; }

    public string BankCode { get; set; } = string.Empty;

    public string PayDate { get; set; } = string.Empty;

    public string SecureHash { get; set; } = string.Empty;

    public bool IsSuccess =>
        ResponseCode == "00" && TransactionStatus == "00";
}
