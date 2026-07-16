using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using BLL.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BLL.Interfaces;

public interface IVnPayGateway
{
    VNPayCreatePaymentResult CreatePaymentUrl(VNPayCreatePaymentRequest request);

    bool ValidateSignature(IDictionary<string, string?> vnpayData);

    string BuildOrderId(int paymentId);
}

public class VnPayGateway : IVnPayGateway
{
    private readonly VNPayOptions _options;
    private readonly ILogger<VnPayGateway> _logger;

    public VnPayGateway(IOptions<VNPayOptions> options, ILogger<VnPayGateway> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public VNPayCreatePaymentResult CreatePaymentUrl(VNPayCreatePaymentRequest request)
    {
        var expireDate = DateTime.UtcNow.AddSeconds(_options.ExpirationSeconds)
            .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var createDate = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? _options.Locale : request.Locale!;
        var amount = (request.Amount * 100).ToString(CultureInfo.InvariantCulture);

        var parameters = new SortedDictionary<string, string>
        {
            ["vnp_Version"] = _options.Version,
            ["vnp_Command"] = _options.Command,
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_Amount"] = amount,
            ["vnp_CurrCode"] = _options.CurrencyCode,
            ["vnp_TxnRef"] = request.OrderId,
            ["vnp_OrderInfo"] = request.OrderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = locale,
            ["vnp_ReturnUrl"] = _options.ReturnUrl,
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_CreateDate"] = createDate,
            ["vnp_ExpireDate"] = expireDate
        };

        if (!string.IsNullOrWhiteSpace(request.BankCode))
        {
            parameters["vnp_BankCode"] = request.BankCode!;
        }

        if (!_options.Enabled)
        {
            var localPayUrl = $"/Payments/DemoVnPay?orderId={Uri.EscapeDataString(request.OrderId)}&amount={amount}&orderInfo={Uri.EscapeDataString(request.OrderInfo)}";
            return new VNPayCreatePaymentResult
            {
                PaymentId = request.PaymentId,
                OrderId = request.OrderId,
                PayUrl = localPayUrl,
                QrCodeUrl = $"/Payments/DemoQr/{request.PaymentId}",
                Message = "Demo mode (VNPay disabled) — open local mock gateway"
            };
        }

        var signData = BuildSignData(parameters);
        var secureHash = HmacSha512(_options.HashSecret, signData);
        parameters["vnp_SecureHash"] = secureHash;

        var queryString = string.Join("&", parameters.Select(kvp =>
            $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

        var payUrl = $"{_options.PaymentUrl}?{queryString}";

        _logger.LogInformation("VNPay payment URL created for order {OrderId}", request.OrderId);

        return new VNPayCreatePaymentResult
        {
            PaymentId = request.PaymentId,
            OrderId = request.OrderId,
            PayUrl = payUrl,
            Message = "OK"
        };
    }

    public bool ValidateSignature(IDictionary<string, string?> vnpayData)
    {
        var secureHash = vnpayData.TryGetValue("vnp_SecureHash", out var hash)
            ? hash ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(secureHash))
        {
            return false;
        }

        var cloned = new SortedDictionary<string, string>();
        foreach (var kvp in vnpayData)
        {
            if (string.IsNullOrEmpty(kvp.Key) || kvp.Key.StartsWith("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            cloned[kvp.Key] = kvp.Value ?? string.Empty;
        }

        var signData = BuildSignData(cloned);
        var expected = HmacSha512(_options.HashSecret, signData);
        return string.Equals(expected, secureHash, StringComparison.OrdinalIgnoreCase);
    }

    public string BuildOrderId(int paymentId)
    {
        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"VNP{ts}_{paymentId:000000}";
    }

    public static string BuildSignedCallback(IDictionary<string, string> parameters, string hashSecret)
    {
        var sorted = new SortedDictionary<string, string>(parameters);
        var signData = BuildSignData(sorted);
        var hash = HmacSha512(hashSecret, signData);
        sorted["vnp_SecureHash"] = hash;
        return string.Join("&", sorted.Select(kvp =>
            $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));
    }

    private static string BuildSignData(SortedDictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var kvp in parameters)
        {
            if (kvp.Key.StartsWith("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!first)
            {
                sb.Append('&');
            }
            sb.Append(WebUtility.UrlEncode(kvp.Key));
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(kvp.Value));
            first = false;
        }
        return sb.ToString();
    }

    private static string HmacSha512(string key, string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(input);
        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(inputBytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
