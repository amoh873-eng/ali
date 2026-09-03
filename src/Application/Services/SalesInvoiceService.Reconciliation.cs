using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// SalesInvoiceService — تسوية مبيعات البطاقات مع كشف البنك.
///
/// يشمل:
/// 1) GetCardReconciliationAsync: مقارنة فواتير البطاقة المسجلة في النظام مع صفوف
///    كشف البنك المستوردة (بالمطابقة عبر رقم المرجع + المبلغ) وتصنيف الحالة.
/// 2) ImportBankStatementAsync: استيراد كشف البنك بصيغة CSV أو XLSX (بدون مكتبات
///    خارجية — يقرأ الـ XLSX كملف ZIP مع sharedStrings + sheet1.xml).
///
/// ⚠️ أمن (PCI-DSS): لا نعالج أو نخزن أبداً رقم بطاقة كاملاً أو CVV أو تاريخ انتهاء.
/// </summary>
public partial class SalesInvoiceService
{
    /// <summary>
    /// يبني جدول تسوية بطاقات الدفع للفترة المحددة.
    /// مصدران: فواتير النظام (PaymentMethod == Card) وصفوف البنك المستوردة.
    /// المطابقة: نفس رقم المرجع (بتجاهل حالة الأحرف والمسافات) والمبلغ ضمن فارق 0.01.
    /// </summary>
    public async Task<CardReconciliationResultDto> GetCardReconciliationAsync(DateTime from, DateTime to)
    {
        var rangeEnd = to.Date.AddDays(1); // شامل نهاية اليوم
        var result = new CardReconciliationResultDto { From = from.Date, To = to.Date };

        var invoices = await _context.Set<SalesInvoice>()
            .Where(i => i.PaymentMethod == SalesPaymentMethod.Card
                        && !i.IsDeleted
                        && i.Status == DocumentStatus.Posted
                        && i.InvoiceDate >= from.Date && i.InvoiceDate < rangeEnd)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.CardApprovalCode,
                i.CardLast4,
                i.CardNetwork,
                i.TotalAmount,
                i.InvoiceDate,
                CustomerName = i.Customer != null ? i.Customer.NameAr : "—"
            })
            .ToListAsync();

        var bankRows = await _context.Set<BankCardStatement>()
            .Where(b => !b.IsDeleted
                        && b.TransactionDate >= from.Date && b.TransactionDate < rangeEnd)
            .ToListAsync();

        var norm = (string s) => (s ?? string.Empty).Trim().ToUpperInvariant();
        var bankByRef = bankRows
            .GroupBy(b => norm(b.Reference))
            .ToDictionary(g => g.Key, g =>
            {
                var list = g.ToList();
                var total = list.Sum(x => x.Amount);
                return (First: list.First(), Total: total, All: list);
            });

        var usedBankRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var inv in invoices)
        {
            var row = new CardReconciliationRowDto
            {
                Reference = inv.CardApprovalCode ?? "—",
                InvoiceNumber = inv.InvoiceNumber,
                CustomerName = inv.CustomerName,
                Last4 = inv.CardLast4,
                Network = inv.CardNetwork,
                SystemAmount = inv.TotalAmount,
                SystemDate = inv.InvoiceDate
            };

            var key = norm(inv.CardApprovalCode ?? "");
            if (key.Length == 0 || !bankByRef.TryGetValue(key, out var bankMatch))
            {
                row.Status = CardReconciliationStatus.NoBankConfirmation;
            }
            else
            {
                usedBankRefs.Add(key);
                row.BankAmount = bankMatch.Total;
                row.BankDate = bankMatch.First.TransactionDate;
                row.Status = Math.Abs(inv.TotalAmount - bankMatch.Total) <= 0.01m
                    ? CardReconciliationStatus.Matched
                    : CardReconciliationStatus.AmountMismatch;
            }

            result.Rows.Add(row);
        }

        // صفوف البنك غير الموجودة في النظام (يجب تحقيقها)
        foreach (var bank in bankRows)
        {
            var key = norm(bank.Reference);
            if (key.Length == 0 || usedBankRefs.Contains(key)) continue;

            result.Rows.Add(new CardReconciliationRowDto
            {
                Reference = bank.Reference,
                InvoiceNumber = null,
                CustomerName = "—",
                BankAmount = bank.Amount,
                BankDate = bank.TransactionDate,
                Status = CardReconciliationStatus.NotInSystem
            });
        }
// الملخص
        foreach (var row in result.Rows)
        {
            switch (row.Status)
            {
                case CardReconciliationStatus.Matched:
                    result.MatchedCount++; result.MatchedAmount += row.SystemAmount ?? 0m; break;
                case CardReconciliationStatus.NoBankConfirmation:
                    result.PendingCount++; result.PendingAmount += row.SystemAmount ?? 0m; break;
                case CardReconciliationStatus.NotInSystem:
                    result.BankOnlyCount++; result.BankOnlyAmount += row.BankAmount ?? 0m; break;
                case CardReconciliationStatus.AmountMismatch:
                    result.MismatchCount++; result.MismatchAmount += row.SystemAmount ?? 0m; break;
            }
        }

        result.SystemCardTotal = invoices.Sum(i => i.TotalAmount);
        result.BankImportedTotal = bankRows.Sum(b => b.Amount);
        result.Rows = result.Rows
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.SystemDate ?? r.BankDate ?? DateTime.MinValue)
            .ToList();

        return result;
    }

    /// <summary>
    /// يستورد كشف تسوية البنك (CSV أو XLSX) ويحفظ كل صف كسجل BankCardStatement.
    /// الصفوف المكررة (نفس رقم المرجع والموجودة أصلاً) تُتخطّى بدل تكرارها.
    /// </summary>
    public async Task<CardBankImportResultDto> ImportBankStatementAsync(Stream fileStream, string fileName, string? importedBy)
    {
        var result = new CardBankImportResultDto();
        if (fileStream is null)
            throw new InvalidOperationException("لم يتم اختيار ملف.");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        List<(string, decimal, DateTime)> parsedRows;

        // دفق المتصفح (IBrowserFile) لا يدعم القراءات المتزامنة — ننسخه غير متزامناً إلى MemoryStream أولاً،
        // ثم نوزّع المحتوى من الذاكرة (حتى يعمل التحليل بأمان داخل Task.Run).
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer);
        buffer.Position = 0;

        if (ext == ".xlsx")
            parsedRows = await Task.Run(() => ParseXlsxRows(buffer));
        else if (ext == ".csv" || ext == ".txt")
            parsedRows = await Task.Run(() => ParseCsvRows(buffer));
        else
            throw new InvalidOperationException("صيغة غير مدعومة — ارفع ملف CSV أو XLSX فقط.");

        result.TotalRows = parsedRows.Count;

        var existingRefs = await _context.Set<BankCardStatement>()
            .Where(b => !b.IsDeleted)
            .Select(b => b.Reference)
            .ToListAsync();
        var existingSet = new HashSet<string>(
            existingRefs.Select(r => r.Trim().ToUpperInvariant()));

        var now = DateTime.UtcNow;
        foreach (var (reference, amount, date) in parsedRows)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                result.Errors.Add("سطر برقم مرجع فارغ تم تجاهله.");
                continue;
            }
            if (amount <= 0)
            {
                result.Errors.Add($"المرجع {reference}: المبلغ غير صالح ({amount}).");
                continue;
            }

            var key = reference.Trim().ToUpperInvariant();
            if (existingSet.Contains(key))
            {
                result.SkippedExisting++;
                continue;
            }

            _context.Set<BankCardStatement>().Add(new BankCardStatement
            {
                Id = Guid.NewGuid(),
                Reference = reference.Trim(),
                Amount = amount,
                TransactionDate = date,
                ImportedAt = now,
                ImportedBy = importedBy,
                CreatedAt = now,
                UpdatedAt = now
            });
            existingSet.Add(key);
            result.ImportedNew++;
            result.ImportedAmount += amount;
        }

        await _context.SaveChangesAsync();
        return result;
    }
}