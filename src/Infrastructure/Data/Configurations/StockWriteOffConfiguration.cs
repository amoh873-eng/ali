using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لكيان StockWriteOff (جدول StockWriteOffs — سندات إتلاف المخزون).</summary>
public class StockWriteOffConfiguration : IEntityTypeConfiguration<StockWriteOff>
{
    public void Configure(EntityTypeBuilder<StockWriteOff> builder)
    {
        builder.ToTable("StockWriteOffs");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.DocumentNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(w => w.DocumentNumber).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.Property(w => w.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(w => w.Reason).IsRequired();
        builder.Property(w => w.OtherReasonText).HasMaxLength(200);
        builder.Property(w => w.UnitCost).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(w => w.Date).IsRequired();
        builder.Property(w => w.Notes).HasMaxLength(500);
        builder.Property(w => w.CreatedByUserId).HasMaxLength(450);

        builder.HasOne(w => w.Item)
            .WithMany()
            .HasForeignKey(w => w.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Warehouse)
            .WithMany()
            .HasForeignKey(w => w.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // الدُفعة المحددة (اختياري) — يبقى مرجعها للتدقيق ومنع الحذف المتتالي
        builder.HasOne(w => w.Batch)
            .WithMany()
            .HasForeignKey(w => w.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        // القيد المحاسبي المرتبط — لا حذف متتالٍ (تدقيق كامل)
        builder.HasOne(w => w.JournalEntry)
            .WithMany()
            .HasForeignKey(w => w.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => new { w.ItemId, w.WarehouseId });
        builder.HasIndex(w => w.Date);
        builder.HasIndex(w => w.Reason);

        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}