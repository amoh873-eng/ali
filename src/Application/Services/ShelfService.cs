using ERPSystem.Application.DTOs.Shelf;
using ERPSystem.Application.Interfaces;
using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// وحدة منسّق الرفوف (المرحلة 4 — الشرحة العمودية):
/// فحص رف بكمية ملاحَظة وصور (تُحفظ Base64 على القرص)، حدود رف مستهدفة،
/// تحذيرات انتهاء قريبة (تعيد استخدام ItemBatch + إعداد ExpiryWarningWindowDays)،
/// ورصد أسعار المنافسين. لا منطق مخزني موازٍ — الإتلاف عبر خدمته الخاصة.
/// </summary>
public class ShelfService : IShelfService
{
    private readonly DbContext _context;
    private readonly ISystemSettingsService _settings;

    public ShelfService(DbContext context, ISystemSettingsService settings)
    {
        _context = context;
        _settings = settings;
    }

    public async Task<List<ShelfParLevelDto>> GetParLevelsAsync(Guid? itemId)
    {
        var query = _context.Set<ShelfParLevel>().Where(p => p.IsActive);
        if (itemId.HasValue) query = query.Where(p => p.ItemId == itemId.Value);
        var rows = await query.OrderBy(p => p.Location).ToListAsync();
        return rows.Select(p => new ShelfParLevelDto
        {
            Id = p.Id,
            ItemId = p.ItemId,
            Location = p.Location,
            ParLevel = p.ParLevel
        }).ToList();
    }

    public async Task<ShelfCheckDto> CreateShelfCheckAsync(CreateShelfCheckDto dto, string createdByUserId)
    {
        if (dto.ItemId is null) throw new InvalidOperationException("يجب تحديد الصنف.");
        if (string.IsNullOrWhiteSpace(dto.Location)) dto.Location = "الرئيسية";

        var check = new ShelfCheck
        {
            Id = Guid.NewGuid(),
            ItemId = (Guid)dto.ItemId,
            Location = dto.Location,
            ObservedQty = dto.ObservedQty,
            ParLevel = dto.ParLevel,
            Notes = dto.Notes,
            CreatedByUserId = createdByUserId,
            CheckedAt = dto.CheckedAt,
            CreatedAt = DateTime.UtcNow
        };
        if (!string.IsNullOrWhiteSpace(dto.PhotoOneBase64)) check.PhotoOnePath = SavePhoto(dto.PhotoOneBase64);
        if (!string.IsNullOrWhiteSpace(dto.PhotoTwoBase64)) check.PhotoTwoPath = SavePhoto(dto.PhotoTwoBase64);

        _context.Set<ShelfCheck>().Add(check);
        await _context.SaveChangesAsync();

        return MapCheck(check);
    }

    public async Task<List<ShelfCheckDto>> GetShelfChecksAsync(Guid? itemId, string? location)
    {
        var query = _context.Set<ShelfCheck>().AsQueryable();
        if (itemId.HasValue) query = query.Where(c => c.ItemId == itemId.Value);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(c => c.Location == location);
        var rows = await query.OrderByDescending(c => c.CheckedAt).ThenByDescending(c => c.CreatedAt).ToListAsync();
        return rows.Select(c => MapCheck(c)).ToList();
    }

public async Task<List<ShelfExpiryWarningDto>> GetExpiryWarningsAsync(Guid? itemId)
    {
        var settings = await _settings.GetAsync();
        var windowDays = settings.ExpiryWarningWindowDays <= 0 ? 7 : settings.ExpiryWarningWindowDays;
        var today = DateTime.Today;
        var fromD = new DateOnly(today.Year, today.Month, today.Day);
        var toD = new DateOnly(today.AddDays(windowDays).Year, today.AddDays(windowDays).Month, today.AddDays(windowDays).Day);

        var batches = _context.Set<ItemBatch>()
            .Where(b => b.Quantity > 0m && b.ExpiryDate.HasValue
                && b.ExpiryDate >= fromD && b.ExpiryDate <= toD)
            .AsQueryable();
        if (itemId.HasValue) batches = batches.Where(b => b.ItemId == itemId.Value);
        var rows = await batches.OrderBy(b => b.ExpiryDate).ToListAsync();

        var itemIds = rows.Select(b => b.ItemId.ToString()).ToList();
        var items = await _context.Set<Item>()
            .Where(i => itemIds.Contains(i.Id.ToString()))
            .ToListAsync();
        var itemById = new Dictionary<string, Item>();
        foreach (var i in items) itemById[i.Id.ToString()] = i;

        var list = new List<ShelfExpiryWarningDto>();
        foreach (var b in rows)
        {
            var item = itemById.TryGetValue(b.ItemId.ToString(), out var iv) ? iv : null;
            list.Add(new ShelfExpiryWarningDto
            {
                ItemId = b.ItemId,
                ItemCode = item?.Code ?? "",
                ItemNameAr = item?.NameAr ?? "",
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate!,
                RemainingQty = b.Quantity,
                DaysLeft = windowDays
            });
        }
        return list;
    }

    public async Task<ShelfPriceCaptureDto> CreatePriceCaptureAsync(CreateShelfPriceCaptureDto dto, string createdByUserId)
    {
        if (dto.ItemId is null) throw new InvalidOperationException("يجب تحديد الصنف.");
        if (dto.Price < 0m) throw new InvalidOperationException("السعر غير صالح.");
        if (string.IsNullOrWhiteSpace(dto.CompetitorName)) dto.CompetitorName = "أخرى";

        var capture = new ShelfPriceCapture
        {
            Id = Guid.NewGuid(),
            ItemId = (Guid)dto.ItemId,
            CompetitorName = dto.CompetitorName,
            Price = dto.Price,
            Notes = dto.Notes,
            CreatedByUserId = createdByUserId,
            CapturedAt = dto.CapturedAt,
            CreatedAt = DateTime.UtcNow
        };
        if (!string.IsNullOrWhiteSpace(dto.PhotoBase64)) capture.PhotoPath = SavePhoto(dto.PhotoBase64);

        _context.Set<ShelfPriceCapture>().Add(capture);
        await _context.SaveChangesAsync();

        return new ShelfPriceCaptureDto
        {
            Id = capture.Id,
            ItemId = capture.ItemId,
            CompetitorName = capture.CompetitorName,
            Price = capture.Price,
            PhotoPath = capture.PhotoPath,
            Notes = capture.Notes,
            CapturedAt = capture.CapturedAt
        };
    }

    public async Task<List<ShelfPriceCaptureDto>> GetPriceCapturesAsync()
    {
        var rows = await _context.Set<ShelfPriceCapture>()
            .OrderByDescending(c => c.CapturedAt)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();
        return rows.Select(c => new ShelfPriceCaptureDto
        {
            Id = c.Id,
            ItemId = c.ItemId,
            CompetitorName = c.CompetitorName,
            Price = c.Price,
            PhotoPath = c.PhotoPath,
            Notes = c.Notes,
            CapturedAt = c.CapturedAt
        }).ToList();
    }

    public async Task<List<ShelfFieldActivityDto>> GetFieldActivityAsync()
    {
        var checks = await _context.Set<ShelfCheck>().ToListAsync();
        var captures = await _context.Set<ShelfPriceCapture>().ToListAsync();
        var collections = await _context.Set<RepCashCustody>().ToListAsync();

        var byUser = new Dictionary<string, ShelfFieldActivityDto>();
        foreach (var c in checks)
        {
            var d = byUser.TryGetValue(c.CreatedByUserId, out var ex) ? ex : AddUser(byUser, c.CreatedByUserId);
            d.ShelfChecks++;
        }
        foreach (var c in captures)
        {
            var d = byUser.TryGetValue(c.CreatedByUserId, out var ex) ? ex : AddUser(byUser, c.CreatedByUserId);
            d.PriceCaptures++;
        }
        foreach (var c in collections)
        {
            if (c.Effect == ERPSystem.Domain.Enums.RepCustodyEffect.Collect)
            {
                var d = byUser.TryGetValue(c.RepUserId, out var ex) ? ex : AddUser(byUser, c.RepUserId);
                d.RepCollections++;
            }
        }
        return byUser.Values.ToList();
    }

    public async Task<List<ShelfShortfallDto>> GetShortfallsAsync()
    {
        // حدود الرف الحالية كخريطة itemId|location → ParLevel (الحد المحدَّث أهَمّ من المذكور في الفحص)
        var parRows = await _context.Set<ShelfParLevel>()
            .Where(p => p.IsActive)
            .ToListAsync();
        var parByKey = new Dictionary<string, decimal>();
        foreach (var p in parRows) parByKey[p.ItemId.ToString() + "|" + p.Location] = p.ParLevel;

        var checks = await _context.Set<ShelfCheck>().ToListAsync();
        var map = new Dictionary<string, ShelfShortfallDto>();
        foreach (var c in checks)
        {
            var key = c.ItemId.ToString() + "|" + c.Location;
            var par = c.ParLevel.HasValue ? c.ParLevel.Value : (parByKey.TryGetValue(key, out var pv) ? pv : 0m);
            if (par <= 0m || c.ObservedQty >= par) continue;

            var d = map.TryGetValue(key, out var ex) ? ex : AddShortfall(map, key, c, par);
            d.BelowCount++;
            d.AvgObserved = Math.Round((d.AvgObserved * (d.BelowCount - 1) + c.ObservedQty) / d.BelowCount, 2);
        }
        return map.Values.ToList();
    }

    private static ShelfFieldActivityDto AddUser(Dictionary<string, ShelfFieldActivityDto> m, string userId)
    {
        var d = new ShelfFieldActivityDto { UserId = userId };
        m[userId] = d;
        return d;
    }

    private static ShelfShortfallDto AddShortfall(Dictionary<string, ShelfShortfallDto> m, string key, ShelfCheck c, decimal par)
    {
        var d = new ShelfShortfallDto { ItemId = c.ItemId, Location = c.Location, ParLevel = par, BelowCount = 0, AvgObserved = 0m };
        m[key] = d;
        return d;
    }

    private static ShelfCheckDto MapCheck(ShelfCheck c) => new()
    {
        Id = c.Id,
        ItemId = c.ItemId,
        Location = c.Location,
        ObservedQty = c.ObservedQty,
        ParLevel = c.ParLevel,
        PhotoOnePath = c.PhotoOnePath,
        PhotoTwoPath = c.PhotoTwoPath,
        Notes = c.Notes,
        CreatedByUserId = c.CreatedByUserId,
        CheckedAt = c.CheckedAt
    };

    private string SavePhoto(string base64)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);
            var path = $"D:/erp_shelf_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg";
            var f = File.OpenWrite(path);
            f.Write(bytes);
            f.Flush();
            f.Dispose();
            return path;
        }
        catch { return null; }
    }
}