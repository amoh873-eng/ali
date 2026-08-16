using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the PurchaseReturn entity (مردودات المشتريات).</summary>
public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("PurchaseReturns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReturnNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.ReturnNumber).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.Property(r => r.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(r => r.TotalAmount).HasColumnType("decimal(18,2)");

        builder.Property(r => r.Note).HasMaxLength(1000);
        builder.Property(r => r.ReturnDate).IsRequired();

        builder.HasOne(r => r.PurchaseInvoice)
            .WithMany(i => i.PurchaseReturns)
            .HasForeignKey(r => r.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Supplier)
            .WithMany(s => s.PurchaseReturns)
            .HasForeignKey(r => r.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.PurchaseReturn)
            .HasForeignKey(l => l.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(r => r.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}