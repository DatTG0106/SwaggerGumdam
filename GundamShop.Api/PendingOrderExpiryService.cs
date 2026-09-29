using GundamShop.Bll;
using GundamShop.Dal;
using Microsoft.EntityFrameworkCore;

namespace GundamShop.Api;

public class PendingOrderExpiryService(IServiceScopeFactory scopes, ILogger<PendingOrderExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
                var shopping = scope.ServiceProvider.GetRequiredService<ShoppingService>();
                var expired = await db.Orders.Where(o => o.Status == "PendingPayment" && o.ExpiresAtUtc <= DateTime.UtcNow)
                    .OrderBy(o => o.ExpiresAtUtc).Take(100).ToListAsync(stoppingToken);
                foreach (var order in expired) await shopping.Release(order, "Expired", "Expired");
            }
            catch (Exception ex) { logger.LogError(ex, "Failed to expire pending orders"); }
            try { if (!await timer.WaitForNextTickAsync(stoppingToken)) break; }
            catch (OperationCanceledException) { break; }
        }
    }
}
