using System.Security.Cryptography;
using GundamShop.Dal;
using Microsoft.EntityFrameworkCore;

namespace GundamShop.Bll;

public class AuthService(ShopDbContext db)
{
    public async Task<AppUser> Register(RegisterRequest input)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        ShopGuard.Require(!await db.Users.AnyAsync(x => x.Email == email), "Email đã được sử dụng.", 409);
        var user = new AppUser { Email = email, FullName = input.FullName.Trim(), PasswordHash = Hash(input.Password) };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<AppUser> Login(LoginRequest input)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
        ShopGuard.Require(user is { IsActive: true } && Verify(input.Password, user.PasswordHash), "Email hoặc mật khẩu không đúng.", 401);
        return user!;
    }

    public async Task<UserDto> Me(Guid userId)
    {
        var user = ShopGuard.Found(await db.Users.FindAsync(userId), "người dùng");
        return new(user.Id, user.Email, user.FullName, user.Role);
    }

    public async Task<IReadOnlyList<AddressDto>> Addresses(Guid userId) => await db.Addresses.AsNoTracking().Where(x => x.UserId == userId)
        .OrderByDescending(x => x.IsDefault).Select(x => new AddressDto(x.Id, x.Recipient, x.Phone, x.Line1, x.Ward, x.District, x.Province, x.IsDefault)).ToListAsync();

    public async Task<AddressDto> Address(Guid userId, Guid id)
    {
        var a = ShopGuard.Found(await db.Addresses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId), "địa chỉ");
        return ToDto(a);
    }

    public async Task<AddressDto> AddAddress(Guid userId, AddressRequest input)
    {
        if (input.IsDefault) await ClearDefault(userId);
        var a = new Address { UserId = userId };
        Copy(input, a);
        if (!await db.Addresses.AnyAsync(x => x.UserId == userId)) a.IsDefault = true;
        db.Addresses.Add(a);
        await db.SaveChangesAsync();
        return ToDto(a);
    }

    public async Task<AddressDto> UpdateAddress(Guid userId, Guid id, AddressRequest input)
    {
        var a = ShopGuard.Found(await db.Addresses.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId), "địa chỉ");
        if (input.IsDefault) await ClearDefault(userId);
        Copy(input, a);
        await db.SaveChangesAsync();
        return ToDto(a);
    }

    public async Task DeleteAddress(Guid userId, Guid id)
    {
        var a = ShopGuard.Found(await db.Addresses.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId), "địa chỉ");
        db.Addresses.Remove(a);
        await db.SaveChangesAsync();
    }

    private async Task ClearDefault(Guid userId)
    {
        foreach (var a in await db.Addresses.Where(x => x.UserId == userId && x.IsDefault).ToListAsync()) a.IsDefault = false;
    }
    private static void Copy(AddressRequest i, Address a)
    {
        a.Recipient = i.Recipient.Trim(); a.Phone = i.Phone.Trim(); a.Line1 = i.Line1.Trim();
        a.Ward = i.Ward.Trim(); a.District = i.District.Trim(); a.Province = i.Province.Trim(); a.IsDefault = i.IsDefault;
    }
    private static AddressDto ToDto(Address a) => new(a.Id, a.Recipient, a.Phone, a.Line1, a.Ward, a.District, a.Province, a.IsDefault);
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
    private static bool Verify(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
