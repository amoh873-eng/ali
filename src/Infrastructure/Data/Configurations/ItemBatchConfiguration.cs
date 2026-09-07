using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لكيان ItemBatch (جدول ItemBatches — تتبّع دُفعات المخزون الاختياري).</summary>
public class ItemBatchConfiguration : IEntityTypeConfiguration<ItemBatch>
{
    public void Configure(EntityTypeBuilder<ItemBatch> builder)
    {
        builder.ToTable("ItemBatches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BatchNumber).HasMaxLength(50);
        builder.Property(b => b.ProductionDate).HasColumnType("date");
        builder.Property(b => b.ExpiryDate).HasColumnType("date");
        builder.Property(b => b.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(b => b.ReceivedDate).IsRequired();
        builder.Property(b => b.LastExpiryNotifiedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(b => b.Item)
            .WithMany()
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Warehouse)
            .WithMany()
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // مرجع فاتورة الشراء — لا حذف متتالٍ (يبقى محفوظاً للتدقيق)
        builder.HasOne(b => b.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(b => b.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.ItemId, b.WarehouseId });
        builder.HasIndex(b => b.ExpiryDate);
    }
}