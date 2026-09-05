using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لكيان دفعات المخزون (StockBatches).</summary>
public class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
    public void Configure(EntityTypeBuilder<StockBatch> builder)
    {
        builder.ToTable("StockBatches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BatchNumber).HasMaxLength(50);
        builder.Property(b => b.ExpiryDate).HasColumnType("date");
        builder.Property(b => b.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(b => b.ReceivedDate).IsRequired();
        builder.Property(b => b.LastExpiryNotifiedAt).HasColumnType("timestamp with time zone");

        // مفتاح أجنبي إلى الصنف والمخزن (ربط كائنات التنقّل بالمفاتيح لتجنّب مفاتيح ظل مكررة)
        builder.HasOne(b => b.Item)
            .WithMany()
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Warehouse)
            .WithMany()
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // فهرس للاستعلام السريع حسب الصنف والمخزن
        builder.HasIndex(b => new { b.ItemId, b.WarehouseId });
        builder.HasIndex(b => b.ExpiryDate);
    }
}