using System.Diagnostics;
using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using finalProject.Authorization;
using finalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace finalProject.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ISubscriptionPackageService _packageService;
    private readonly IVnPayGateway _vnPayGateway;
    private readonly VNPayOptions _vnPayOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public PaymentsController(
        IPaymentService paymentService,
        ISubscriptionPackageService packageService,
        IVnPayGateway vnPayGateway,
        IOptions<VNPayOptions> vnPayOptions,
        IHttpClientFactory httpClientFactory)
    {
        _paymentService = paymentService;
        _packageService = packageService;
        _vnPayGateway = vnPayGateway;
        _vnPayOptions = vnPayOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    [Authorize(Roles = StudyMateRoles.Admin)]
    public async Task<IActionResult> Manage(CancellationToken cancellationToken)
    {
        var payments = await _paymentService.GetAllPaymentsAsync(GetCurrentUser(), cancellationToken);
        return View(payments);
    }

    [Authorize(Roles = StudyMateRoles.Teacher + "," + StudyMateRoles.Student)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        var payments = await _paymentService.GetMyPaymentsAsync(currentUser, cancellationToken);
        var subscription = await _paymentService.GetMyActiveSubscriptionAsync(currentUser, cancellationToken);
        var packages = await _packageService.GetPackagesAsync(activeOnly: true, cancellationToken);

        ViewData["ActiveSubscription"] = subscription;
        ViewData["Packages"] = packages;

        return View(payments);
    }

    [Authorize(Roles = StudyMateRoles.Teacher + "," + StudyMateRoles.Student)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Buy(int packageId, CancellationToken cancellationToken)
    {
        try
        {
            var ipAddress = GetClientIpAddress();
            var result = await _paymentService.CreatePaymentAsync(GetCurrentUser(), packageId, ipAddress, cancellationToken);
            if (!result.Success || string.IsNullOrWhiteSpace(result.PayUrl))
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tạo được giao dịch VNPay.";
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Checkout), new { id = result.PaymentId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = StudyMateRoles.Teacher + "," + StudyMateRoles.Student)]
    public async Task<IActionResult> Checkout(int id, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        var payment = await _paymentService.GetPaymentByIdAsync(currentUser, id, cancellationToken);
        if (payment is null)
        {
            return NotFound();
        }

        if (payment.UserId != currentUser.UserId)
        {
            return Forbid();
        }

        return View(payment);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var data = Request.Query.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value.ToString());
        if (!_vnPayGateway.ValidateSignature(data))
        {
            TempData["ErrorMessage"] = "Chữ ký VNPay không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var payment = await _paymentService.HandleVnPayReturnAsync(data, cancellationToken);
        if (payment is null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng VNPay.";
            return RedirectToAction(nameof(Index));
        }

        if (payment.Status == PaymentStatus.Completed)
        {
            TempData["StatusMessage"] = $"Thanh toán VNPay #{payment.Id} đã được xác nhận. Gói đã được kích hoạt.";
        }
        else if (payment.Status == PaymentStatus.Failed)
        {
            TempData["ErrorMessage"] = $"Thanh toán VNPay #{payment.Id} thất bại. {payment.Note}";
        }
        else
        {
            TempData["StatusMessage"] = $"Đang chờ VNPay xác nhận đơn #{payment.Id}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        try
        {
            var data = Request.Query.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value.ToString());
            if (!_vnPayGateway.ValidateSignature(data))
            {
                return Json(new { RspCode = "97", Message = "Invalid signature" });
            }

            await _paymentService.HandleVnPayIpnAsync(data, cancellationToken);
            return Json(new { RspCode = "00", Message = "Confirm Success" });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"VNPay IPN error: {ex.Message}");
            return Json(new { RspCode = "99", Message = ex.Message });
        }
    }

    [HttpGet("/Payments/DemoVnPay")]
    [AllowAnonymous]
    public IActionResult DemoVnPay([FromQuery] string orderId, [FromQuery] string amount, [FromQuery] string orderInfo)
    {
        var model = new DemoVnPayViewModel
        {
            OrderId = orderId,
            Amount = amount,
            OrderInfo = orderInfo,
            ReturnUrl = _vnPayOptions.ReturnUrl
        };
        return View(model);
    }

    [HttpPost("/Payments/DemoVnPayPay")]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public IActionResult DemoVnPayPay([FromForm] string orderId, [FromForm] string amount, [FromForm] string orderInfo, [FromForm] string returnUrl, [FromForm] bool success)
    {
        var txnRef = DateTime.UtcNow.Ticks.ToString();
        var responseCode = success ? "00" : "24";
        var payDate = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        var parameters = new Dictionary<string, string>
        {
            ["vnp_TmnCode"] = _vnPayOptions.TmnCode,
            ["vnp_TxnRef"] = orderId,
            ["vnp_TransactionNo"] = txnRef,
            ["vnp_ResponseCode"] = responseCode,
            ["vnp_TransactionStatus"] = responseCode,
            ["vnp_Amount"] = amount,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_BankCode"] = "DEMOBANK",
            ["vnp_PayDate"] = payDate,
            ["vnp_CurrCode"] = _vnPayOptions.CurrencyCode
        };

        var queryString = VnPayGateway.BuildSignedCallback(parameters, _vnPayOptions.HashSecret);
        var redirectUrl = $"{returnUrl}?{queryString}";
        return Redirect(redirectUrl);
    }

    [Authorize(Roles = StudyMateRoles.Teacher + "," + StudyMateRoles.Student)]
    [HttpGet("Payments/DemoQr/{id:int}")]
    public async Task<IActionResult> DemoQr(int id, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        var paymentCheck = await _paymentService.GetPaymentByIdAsync(currentUser, id, cancellationToken);
        if (paymentCheck is null)
        {
            return NotFound();
        }
        if (currentUser.Role != UserRole.Admin && paymentCheck.UserId != currentUser.UserId)
        {
            return Forbid();
        }

        var qrPayload =
            $"VNPAY-{paymentCheck.Id}|{paymentCheck.GatewayOrderId}|{paymentCheck.Amount:0}|{paymentCheck.SubscriptionPackageName}|StudyMate";

        var qrUrl =
            $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(qrPayload)}";

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(qrUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Redirect(qrUrl);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return File(bytes, "image/png");
        }
        catch
        {
            return Redirect(qrUrl);
        }
    }

    [Authorize(Roles = StudyMateRoles.Admin + "," + StudyMateRoles.Teacher + "," + StudyMateRoles.Student)]
    [HttpPost("Payments/DemoConfirm/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoConfirm(int id, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        try
        {
            await _paymentService.DemoConfirmAsync(currentUser, id, "Xác nhận demo thành công", cancellationToken);
            TempData["StatusMessage"] = $"Đã xác nhận thanh toán demo cho đơn #{id}. Gói đã được kích hoạt.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private string GetClientIpAddress()
    {
        var forwarded = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    private CurrentUserDto GetCurrentUser()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = User.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(userIdValue, out var userId) ||
            !Enum.TryParse<UserRole>(roleValue, out var role))
        {
            throw new InvalidOperationException("Current user claims are invalid.");
        }

        return new CurrentUserDto { UserId = userId, Role = role };
    }
}
