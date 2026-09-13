using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لكيان وردية الكاشير (CashierShift).</summary>
public class CashierShiftConfiguration : IEntityTypeConfiguration<CashierShift>
{
    public void Configure(EntityTypeBuilder<CashierShift> builder)
    {
        builder.ToTable("CashierShifts");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.CashierUserId).IsRequired().HasMaxLength(450);
        builder.Property(t => t.StartTime).IsRequired();
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.OpeningFloatAmount).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(t => t.OpeningTransferId).IsRequired();
        builder.Property(t => t.ExpectedCashAmount).HasColumnType("numeric(18,2)");
        builder.Property(t => t.CountedCashAmount).HasColumnType("numeric(18,2)");
        builder.Property(t => t.VarianceAmount).HasColumnType("numeric(18,2)");
        builder.Property(t => t.ClosedByUserId).HasMaxLength(450);
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // معاملة الفتح لمرة واحدة لكل وردية
        builder.HasOne<CashDrawerTransaction>()
            .WithMany()
            .HasForeignKey("OpeningTransferId")
            .OnDelete(DeleteBehavior.Restrict);

        // قيد الفرق عند الإغلاق (إن وُجد)
        builder.HasOne(t => t.JournalEntry)
            .WithMany()
            .HasForeignKey(t => t.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.CashierUserId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.StartTime);
        builder.HasIndex(t => t.JournalEntryId);
        builder.HasIndex(t => t.OpeningTransferId);
    }
}