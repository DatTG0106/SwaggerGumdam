using GundamShop.Dal;
using Microsoft.EntityFrameworkCore;

namespace GundamShop.Bll;

public class PromotionService(ShopDbContext db)
{
    public Task<Page<VoucherDto>> Vouchers(int page, int size) => PageHelper.GetAsync(db.Vouchers.AsNoTracking().OrderBy(x => x.Code)
        .Select(x => new VoucherDto(x.Id, x.Code, x.DiscountVnd, x.MinSubtotalVnd, x.MaxUses, x.UsedCount, x.StartsAtUtc, x.EndsAtUtc, x.IsActive)), page, size);

    public async Task<VoucherDto> Voucher(Guid id)
    {
        var x = ShopGuard.Found(await db.Vouchers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id), "voucher");
        return ToDto(x);
    }

    public async Task<VoucherDto> AddVoucher(VoucherRequest i)
    {
        Validate(i);
        var code = i.Code.Trim().ToUpperInvariant();
        ShopGuard.Require(!await db.Vouchers.AnyAsync(x => x.Code == code), "Mã voucher đã tồn tại.", 409);
        var x = new Voucher(); Copy(i, x);
        db.Vouchers.Add(x); await db.SaveChangesAsync(); return ToDto(x);
    }

    public async Task<VoucherDto> UpdateVoucher(Guid id, VoucherRequest i)
    {
        Validate(i);
        var x = ShopGuard.Found(await db.Vouchers.FindAsync(id), "voucher");
        var code = i.Code.Trim().ToUpperInvariant();
        ShopGuard.Require(!await db.Vouchers.AnyAsync(v => v.Id != id && v.Code == code), "Mã voucher đã tồn tại.", 409);
        ShopGuard.Require(i.MaxUses >= x.UsedCount, "MaxUses không thể nhỏ hơn số lượt đã dùng.");
        Copy(i, x); await db.SaveChangesAsync(); return ToDto(x);
    }

    public async Task DeleteVoucher(Guid id)
    {
        var x = ShopGuard.Found(await db.Vouchers.FindAsync(id), "voucher");
        x.IsActive = false; await db.SaveChangesAsync();
    }

    public Task<Page<ReviewDto>> Reviews(Guid productId, int page, int size) => PageHelper.GetAsync(db.Reviews.AsNoTracking()
        .Where(x => x.ProductId == productId).OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new ReviewDto(x.Id, x.ProductId, x.UserId, x.Rating, x.Comment, x.CreatedAtUtc)), page, size);

    public async Task<ReviewDto> Review(Guid id)
    {
        var x = ShopGuard.Found(await db.Reviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id), "đánh giá");
        return ToDto(x);
    }

    public async Task<ReviewDto> AddReview(Guid userId, ReviewRequest i)
    {
        ShopGuard.Require(await db.Products.AnyAsync(x => x.Id == i.ProductId), "Sản phẩm không tồn tại.");
        ShopGuard.Require(!await db.Reviews.AnyAsync(x => x.ProductId == i.ProductId && x.UserId == userId), "Bạn đã đánh giá sản phẩm.", 409);
        var bought = await db.Orders.Where(o => o.UserId == userId && o.Status == "Delivered")
            .Join(db.OrderItems, o => o.Id, oi => oi.OrderId, (o, oi) => oi.VariantId)
            .Join(db.Variants, id => id, v => v.Id, (id, v) => v.ProductId)
            .AnyAsync(id => id == i.ProductId);
        ShopGuard.Require(bought, "Chỉ được đánh giá sản phẩm đã nhận.", 403);
        var x = new Review { UserId = userId, ProductId = i.ProductId, Rating = i.Rating, Comment = i.Comment.Trim() };
        db.Reviews.Add(x); await db.SaveChangesAsync(); return ToDto(x);
    }

    public async Task<ReviewDto> UpdateReview(Guid userId, Guid id, ReviewUpdateRequest i)
    {
        var x = ShopGuard.Found(await db.Reviews.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId), "đánh giá");
        x.Rating = i.Rating; x.Comment = i.Comment.Trim(); await db.SaveChangesAsync(); return ToDto(x);
    }

    public async Task DeleteReview(Guid userId, Guid id, bool admin)
    {
        var x = ShopGuard.Found(await db.Reviews.SingleOrDefaultAsync(x => x.Id == id && (admin || x.UserId == userId)), "đánh giá");
        db.Reviews.Remove(x); await db.SaveChangesAsync();
    }

    private static void Validate(VoucherRequest i) => ShopGuard.Require(i.StartsAtUtc < i.EndsAtUtc, "Khoảng thời gian voucher không hợp lệ.");
    private static void Copy(VoucherRequest i, Voucher x)
    {
        x.Code = i.Code.Trim().ToUpperInvariant(); x.DiscountVnd = i.DiscountVnd;
        x.MinSubtotalVnd = i.MinSubtotalVnd; x.MaxUses = i.MaxUses;
        x.StartsAtUtc = i.StartsAtUtc.ToUniversalTime(); x.EndsAtUtc = i.EndsAtUtc.ToUniversalTime();
    }
    private static VoucherDto ToDto(Voucher x) => new(x.Id, x.Code, x.DiscountVnd, x.MinSubtotalVnd, x.MaxUses, x.UsedCount, x.StartsAtUtc, x.EndsAtUtc, x.IsActive);
    private static ReviewDto ToDto(Review x) => new(x.Id, x.ProductId, x.UserId, x.Rating, x.Comment, x.CreatedAtUtc);
}
