using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لكيان تحويلات النقد بين الخزنة ودرج الكاش (CashDrawerTransactions).</summary>
public class CashDrawerTransactionConfiguration : IEntityTypeConfiguration<CashDrawerTransaction>
{
    public void Configure(EntityTypeBuilder<CashDrawerTransaction> builder)
    {
        builder.ToTable("CashDrawerTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(t => t.Timestamp).IsRequired();
        builder.Property(t => t.CashierUserId).IsRequired().HasMaxLength(450);
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();

        // القيد المحاسبي المرتبط بالتحويل (إلزامي — كل تحويل يرحّل قيداً متوازناً).
        builder.HasOne(t => t.JournalEntry)
            .WithMany()
            .HasForeignKey(t => t.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // فهارس لاستعلامات سجل التحويلات (حسب التاريخ / الكاشير).
        builder.HasIndex(t => t.Timestamp);
        builder.HasIndex(t => t.CashierUserId);
        builder.HasIndex(t => t.JournalEntryId);
    }
}
