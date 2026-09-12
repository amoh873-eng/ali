using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the SalesInvoice entity (فواتير المبيعات).
/// </summary>
public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("SalesInvoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(i => new { i.CustomerId, i.InvoiceDate });

        // Financial fields: 18 digits total, 2 decimals
        builder.Property(i => i.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(i => i.DiscountPercentage).HasColumnType("decimal(18,2)");
        builder.Property(i => i.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(i => i.TaxRate).HasColumnType("decimal(18,2)");
        builder.Property(i => i.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(i => i.PaidAmount).HasColumnType("decimal(18,2)");

        builder.Property(i => i.Note).HasMaxLength(1000);
        builder.Property(i => i.InvoiceDate).IsRequired();
        builder.Property(i => i.IsPos).HasDefaultValue(false);

        // ── Card payment fields (backward-compatible: nullable / default) ──
        builder.Property(i => i.PaymentMethod)
            .HasDefaultValue(Domain.Enums.SalesPaymentMethod.Cash);
        builder.Property(i => i.CardApprovalCode).HasMaxLength(50);
        builder.Property(i => i.CardLast4).HasMaxLength(10);
        builder.Property(i => i.CardNetwork).HasMaxLength(30);
        builder.Property(i => i.CardTransactionAt);

        // Relationships
        builder.HasOne(i => i.Customer)
            .WithMany(c => c.SalesInvoices)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Warehouse)
            .WithMany()
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lines cascade-delete with their invoice (deleting the invoice removes its lines)
        builder.HasMany(i => i.Lines)
            .WithOne(l => l.SalesInvoice)
            .HasForeignKey(l => l.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // The journal entries are optional FKs (null while draft), Restrict prevents
        // accidental deletion of posted entries when the invoice is removed.
        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(i => i.SalesJournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(i => i.CogsJournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.JoFotaraQrCode).HasColumnType("text");
        builder.Property(i => i.JoFotaraReferenceNumber).HasMaxLength(200);
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.CreatedBy).HasMaxLength(100);
        builder.Property(i => i.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
