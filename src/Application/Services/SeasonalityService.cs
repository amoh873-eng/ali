using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// الموسمية — قراءة فقط. يجمع صافي المبيعات (فواتير غير ملغاة) لكل (سنة، شهر)
/// ويعيد مصفوفة سنوات × 12 شهراً لعرض مخطط أعمدة مجمّعة.
/// </summary>
public static class SeasonalityService
{
    private static readonly string[] Palette =
    {
        "#8B5CF6", "#10B981", "#F59E0B", "#EF4444", "#06B6D4",
        "#EC4899", "#6366F1", "#14B8A6", "#F97316", "#84CC16"
    };

    public static async Task<SeasonalityDto> ComputeAsync(DbContext db)
    {
        var invoices = await db.Set<SalesInvoice>()
            .Where(s => !s.IsDeleted && s.Status != DocumentStatus.Cancelled)
            .Select(s => new { s.InvoiceDate, s.TotalAmount })
            .ToListAsync();

        var raw = invoices
            .GroupBy(s => new { s.InvoiceDate.Year, Month = s.InvoiceDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Sum = g.Sum(x => x.TotalAmount) })
            .ToList();

        var years = raw.Select(r => r.Year).Distinct().OrderBy(y => y).ToList();
        var dto = new SeasonalityDto { Years = years };
        for (int i = 0; i < years.Count; i++)
        {
            var series = new SeasonalitySeries
            {
                Year = years[i],
                Color = Palette[i % Palette.Length]
            };
            foreach (var r in raw.Where(r => r.Year == years[i]))
                series.Months[r.Month - 1] = Math.Round(r.Sum, 2);
            dto.Series.Add(series);
            dto.YearTotals.Add(Math.Round(series.Total, 2));
        }
        dto.GrandTotal = Math.Round(dto.Series.Sum(s => s.Total), 2);
        return dto;
    }
}