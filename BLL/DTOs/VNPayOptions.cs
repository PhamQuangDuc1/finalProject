namespace BLL.DTOs;

public class VNPayOptions
{
    public string TmnCode { get; set; } = string.Empty;

    public string HashSecret { get; set; } = string.Empty;

    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    public string ReturnUrl { get; set; } = string.Empty;

    public string IpnUrl { get; set; } = string.Empty;

    public string Version { get; set; } = "2.1.0";

    public string Command { get; set; } = "pay";

    public string CurrencyCode { get; set; } = "VND";

    public string Locale { get; set; } = "vn";

    public int ExpirationSeconds { get; set; } = 900;

    public bool Enabled { get; set; } = true;
}
