using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities;

/// <summary>
/// Represents a journal entry (قيد محاسبي) — the core of double-entry accounting.
///
/// المبدأ المحاسبي الأساسي (لماذا القيد متوازن؟):
/// في نظام القيد المزدوج (Double-Entry) يجب أن يكون مجموع طرفي المدين
/// مساوياً تماماً لمجموع طرفي الدائن، وإلا رُفض القيد. هذا الضمان هو ما
/// يحافظ على توازن الميزانية دائماً، فكل عملية لها أثر مزدوج متساوٍ.
/// </summary>
public class JournalEntry : BaseEntity
{
    /// <summary>
    /// Unique human-readable entry number (e.g., "JE-20260201-0001").
    /// </summary>
    public string EntryNumber { get; set; } = string.Empty;

    /// <summary>
    /// Source of this entry (manual, sales invoice, return, ...).
    /// </summary>
    public JournalEntryType EntryType { get; set; }

    /// <summary>
    /// Business date of the entry.
    /// </summary>
    public DateTime EntryDate { get; set; }

    /// <summary>
    /// Reference to the originating document (e.g., the invoice number).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Human-readable description of the entry.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Line items (debit/credit legs) of this entry.
    /// </summary>
    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}
