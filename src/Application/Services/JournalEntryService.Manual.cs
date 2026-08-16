using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// JournalEntryService — إنشاء وحفظ القيد اليدوي (Manual Entry).
/// </summary>
public partial class JournalEntryService
{
    public async Task<JournalEntryDto> CreateManualEntryAsync(
        DateTime entryDate,
        string? reference,
        string? description,
        IReadOnlyCollection<JournalEntryLegInput> legs)
    {
        // PrepareEntryAsync يتحقق من التوازن، يبني القيد وبنوده، ويحدّث أرصدة الحسابات.
        var entry = await PrepareEntryAsync(
            JournalEntryType.Manual, entryDate, reference, description, legs);

        // القيد اليدوي لا يرتبط بمستند آخر، لذا نحفظه هنا مباشرة (ذرية).
        await _context.SaveChangesAsync();

        // نعيد قراءة القيد مع أسماء الحسابات لعرضه في الواجهة.
        var dto = await GetByIdAsync(entry.Id);
        return dto ?? throw new InvalidOperationException("فشل إنشاء القيد.");
    }
}