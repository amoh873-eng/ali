using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// JournalEntryService — helper/partial part (رقم القيد، التنقل إلى DTO).
/// </summary>
public partial class JournalEntryService
{
    private Task<string> NextNumberAsync(string prefix)
        => NumberSequenceHelper.NextAsync(_context, prefix);

    private static JournalEntryDto MapToDto(JournalEntry entry)
    {
        var totalDebit = entry.Lines.Sum(l => l.DebitAmount);
        var totalCredit = entry.Lines.Sum(l => l.CreditAmount);

        return new JournalEntryDto
        {
            Id = entry.Id,
            EntryNumber = entry.EntryNumber,
            EntryType = (int)entry.EntryType,
            EntryTypeNameAr = GetEntryTypeNameAr(entry.EntryType),
            EntryDate = entry.EntryDate,
            ReferenceNumber = entry.ReferenceNumber,
            Description = entry.Description,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            Lines = entry.Lines
                .Select(l => new JournalEntryLineDto
                {
                    Id = l.Id,
                    AccountId = l.AccountId,
                    AccountCode = l.Account?.Code ?? "—",
                    AccountNameAr = l.Account?.NameAr ?? "—",
                    DebitAmount = l.DebitAmount,
                    CreditAmount = l.CreditAmount,
                    Note = l.Note
                })
                .ToList()
        };
    }

    private static string GetEntryTypeNameAr(JournalEntryType type)
    {
        return type switch
        {
            JournalEntryType.Manual => "قيد يدوي",
            JournalEntryType.SalesInvoice => "فاتورة بيع",
            JournalEntryType.SalesReturn => "مردود مبيعات",
            JournalEntryType.PurchaseInvoice => "فاتورة مشتريات",
            JournalEntryType.PurchaseReturn => "مردود مشتريات",
            JournalEntryType.StockAdjustment => "تسوية مخزون",
            _ => "غير معروف"
        };
    }
}