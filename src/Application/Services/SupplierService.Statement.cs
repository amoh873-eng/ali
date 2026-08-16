using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Application.Services;

/// <summary>
/// SupplierService — كشف حساب المورد.
/// الفاتورة تزيد المستحق للمورد (دائن)، والمردود ينقصه (مدين).
/// </summary>
public partial class SupplierService
{
    public async Task<SupplierStatementDto> GetStatementAsync(Guid supplierId, DateTime from, DateTime to)
    {
        var supplier = await _context.Set<Supplier>()
            .FirstOrDefaultAsync(s => s.Id == supplierId && !s.IsDeleted);
        if (supplier is null) throw new InvalidOperationException("المورد غير موجود");

        var invoices = await _context.Set<PurchaseInvoice>()
            .Where(i => i.SupplierId == supplierId
                        && i.Status == DocumentStatus.Posted
                        && i.InvoiceDate.Date >= from.Date
                        && i.InvoiceDate.Date <= to.Date)
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync();

        var returns = await _context.Set<PurchaseReturn>()
            .Where(r => r.SupplierId == supplierId
                        && r.Status == DocumentStatus.Posted
                        && r.ReturnDate.Date >= from.Date
                        && r.ReturnDate.Date <= to.Date)
            .OrderBy(r => r.ReturnDate)
            .ToListAsync();

        var operations = new List<(DateTime Date, int Seq, string Number, string TypeNameAr, decimal Debit, decimal Credit)>();

        operations.AddRange(invoices.Select(i => (i.InvoiceDate, 0, i.InvoiceNumber, "فاتورة مشتريات", 0m, i.TotalAmount)));
        operations.AddRange(returns.Select(r => (r.ReturnDate, 1, r.ReturnNumber, "مردود مشتريات", r.TotalAmount, 0m)));

        var ordered = operations
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Seq)
            .ThenBy(o => o.Number);

        var lines = new List<SupplierStatementLineDto>();
        var running = 0m;

        foreach (var op in ordered)
        {
            running += op.Credit - op.Debit;
            lines.Add(new SupplierStatementLineDto
            {
                Date = op.Date,
                DocumentNumber = op.Number,
                TypeNameAr = op.TypeNameAr,
                Debit = op.Debit,
                Credit = op.Credit,
                Balance = Math.Round(running, 2)
            });
        }

        return new SupplierStatementDto
        {
            SupplierId = supplier.Id,
            SupplierCode = supplier.Code,
            SupplierName = supplier.NameAr,
            From = from,
            To = to,
            TotalDebit = Math.Round(lines.Sum(l => l.Debit), 2),
            TotalCredit = Math.Round(lines.Sum(l => l.Credit), 2),
            ClosingBalance = Math.Round(running, 2),
            Lines = lines
        };
    }
}