using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GundamShop.Dal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GundamShop.Bll;

public record PaymentSessionDto(Guid OrderId, string Provider, string Status, string? CheckoutUrl);

public class PaymentService(ShopDbContext db, IConfiguration config, IHttpClientFactory clients, Microsoft.Extensions.Hosting.IHostEnvironment env)
{
    public string Provider => config["Payments:Provider"] ?? "Mock";

    public async Task<PaymentSessionDto> Start(Guid userId, Guid orderId)
    {
        var order = ShopGuard.Found(await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId && o.UserId == userId), "đơn hàng");
        ShopGuard.Require(order.Status == "PendingPayment", "Đơn không chờ thanh toán.", 409);
        var payment = await db.Payments.SingleAsync(p => p.OrderId == orderId);
        if (Provider == "Mock") return new(orderId, "Mock", payment.Status, null);
        ShopGuard.Require(Provider == "Stripe", "Payment provider không hợp lệ.", 503);
        if (order.TotalVnd == 0)
        {
            await Complete(orderId, true);
            return new(orderId, "Stripe", "Paid", null);
        }
        if (payment.ExternalSessionId != null) return new(orderId, "Stripe", payment.Status, payment.CheckoutUrl);
        ShopGuard.Require(order.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(30), "Đơn sắp hết hạn, hãy tạo đơn mới.", 409);
        var key = config["Payments:Stripe:SecretKey"];
        var testKey = key?.StartsWith("sk_test_", StringComparison.Ordinal) == true;
        var liveKey = !env.IsDevelopment() && config.GetValue<bool>("Payments:Stripe:AllowLive") && key?.StartsWith("sk_live_", StringComparison.Ordinal) == true;
        ShopGuard.Require(testKey || liveKey, "Cần Stripe secret key hợp lệ; live key chỉ được bật trong Production bằng Payments:Stripe:AllowLive.", 503);
        var successUrl = config["Payments:Stripe:SuccessUrl"];
        var cancelUrl = config["Payments:Stripe:CancelUrl"];
        ShopGuard.Require(Uri.TryCreate(successUrl, UriKind.Absolute, out _) && Uri.TryCreate(cancelUrl, UriKind.Absolute, out _), "Cần cấu hình Stripe SuccessUrl và CancelUrl.", 503);
        var form = new Dictionary<string, string>
        {
            ["mode"] = "payment", ["client_reference_id"] = order.Id.ToString(),
            ["success_url"] = successUrl!, ["cancel_url"] = cancelUrl!,
            ["line_items[0][price_data][currency]"] = "vnd",
            ["line_items[0][price_data][unit_amount]"] = order.TotalVnd.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = $"Gundam Shop order {order.Id}",
            ["line_items[0][quantity]"] = "1",
            ["metadata[orderId]"] = order.Id.ToString(),
            ["expires_at"] = new DateTimeOffset(order.ExpiresAtUtc).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions")
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Headers.Add("Idempotency-Key", $"gundam-order-{order.Id}");
        using var response = await clients.CreateClient("stripe").SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        ShopGuard.Require(response.IsSuccessStatusCode, $"Stripe từ chối tạo Checkout Session: {(int)response.StatusCode}.", 502);
        using var doc = JsonDocument.Parse(body);
        var sessionId = doc.RootElement.GetProperty("id").GetString();
        var url = doc.RootElement.GetProperty("url").GetString();
        ShopGuard.Require(!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(url), "Stripe không trả session hợp lệ.", 502);
        payment.ExternalSessionId = sessionId; payment.CheckoutUrl = url;
        await db.SaveChangesAsync();
        return new(orderId, "Stripe", payment.Status, url);
    }

    public async Task CompleteMock(Guid userId, Guid orderId, bool succeeded)
    {
        ShopGuard.Require(Provider == "Mock", "Mock payment đang tắt.", 404);
        ShopGuard.Require(await db.Orders.AnyAsync(x => x.Id == orderId && x.UserId == userId), "Không tìm thấy đơn hàng.", 404);
        await Complete(orderId, succeeded);
    }

    public async Task<bool> HandleStripeWebhook(string payload, string signature)
    {
        ShopGuard.Require(Provider == "Stripe", "Stripe đang tắt.", 404);
        var secret = config["Payments:Stripe:WebhookSecret"];
        ShopGuard.Require(secret?.StartsWith("whsec_", StringComparison.Ordinal) == true, "Thiếu webhook secret.", 503);
        ShopGuard.Require(VerifySignature(payload, signature, secret!), "Chữ ký Stripe không hợp lệ.", 400);
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var eventId = root.GetProperty("id").GetString() ?? "";
        var type = root.GetProperty("type").GetString() ?? "";
        ShopGuard.Require(eventId.Length > 0, "Webhook thiếu event id.");
        var session = root.GetProperty("data").GetProperty("object");
        var sessionId = session.GetProperty("id").GetString();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.PaymentWebhookEvents.AnyAsync(e => e.EventId == eventId)) return false;
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.ExternalSessionId == sessionId && p.Provider == "Stripe");
        ShopGuard.Require(payment != null, "Stripe session chưa được ghi nhận.", 400);
        var order = ShopGuard.Found(await db.Orders.FindAsync(payment!.OrderId), "đơn hàng");
        if (type is "checkout.session.completed" or "checkout.session.async_payment_succeeded")
        {
            var paid = session.TryGetProperty("payment_status", out var status) && status.GetString() == "paid";
            if (paid) await SetPaymentResult(order, payment, true);
        }
        else if (type is "checkout.session.async_payment_failed" or "checkout.session.expired")
            await SetPaymentResult(order, payment, false);
        db.PaymentWebhookEvents.Add(new PaymentWebhookEvent { EventId = eventId });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return true;
    }

    private async Task Complete(Guid orderId, bool succeeded)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = ShopGuard.Found(await db.Orders.FindAsync(orderId), "đơn hàng");
        var payment = await db.Payments.SingleAsync(p => p.OrderId == orderId);
        ShopGuard.Require(payment.Provider == Provider, "Provider không khớp.", 409);
        await SetPaymentResult(order, payment, succeeded);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    private async Task SetPaymentResult(ShopOrder order, Payment payment, bool succeeded)
    {
        if (order.Status != "PendingPayment")
        {
            if (succeeded && order.Status != "Paid" && order.Status != "Shipped" && order.Status != "Delivered")
            {
                order.PaymentStatus = "RefundRequired";
                payment.Status = "RefundRequired";
            }
            return;
        }
        if (succeeded)
        {
            order.Status = "Paid"; order.PaymentStatus = "Paid"; payment.Status = "Paid";
        }
        else
        {
            order.Status = "PaymentFailed"; order.PaymentStatus = "Failed"; payment.Status = "Failed";
            var items = await db.OrderItems.Where(x => x.OrderId == order.Id).ToListAsync();
            foreach (var item in items)
            {
                var variant = ShopGuard.Found(await db.Variants.FindAsync(item.VariantId), "SKU");
                variant.Stock += item.Quantity;
            }
            if (order.VoucherId is Guid voucherId)
            {
                var voucher = ShopGuard.Found(await db.Vouchers.FindAsync(voucherId), "voucher");
                voucher.UsedCount--;
            }
        }
        payment.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static bool VerifySignature(string payload, string header, string secret)
    {
        var parts = header.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('=', 2)).Where(x => x.Length == 2).ToList();
        var stamp = parts.FirstOrDefault(x => x[0] == "t")?.ElementAtOrDefault(1);
        if (!long.TryParse(stamp, out var unix) || Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unix) > 300) return false;
        var signed = Encoding.UTF8.GetBytes($"{stamp}.{payload}");
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signed);
        foreach (var signature in parts.Where(x => x[0] == "v1").Select(x => x[1]))
        {
            try { if (CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(signature))) return true; }
            catch (FormatException) { }
        }
        return false;
    }
}
