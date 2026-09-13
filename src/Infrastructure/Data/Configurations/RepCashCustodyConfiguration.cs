using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لكيان عهدة نقدية بيد المندوبين (RepCashCustody).</summary>
public class RepCashCustodyConfiguration : IEntityTypeConfiguration<RepCashCustody>
{
    public void Configure(EntityTypeBuilder<RepCashCustody> builder)
    {
        builder.ToTable("RepCashCustody");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.RepUserId).IsRequired().HasMaxLength(450);
        builder.Property(t => t.Effect).IsRequired();
        builder.Property(t => t.PaymentMethod).IsRequired();
        builder.Property(t => t.Amount).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(t => t.Timestamp).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();

        // القيد المحاسبي المرتبط بالعملية (إلزامي — كل عملية ترحّل قيداً متوازناً)
        builder.HasOne(t => t.JournalEntry)
            .WithMany()
            .HasForeignKey(t => t.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // فهارس السجل (حسب المندوب/التاريخ)
        builder.HasIndex(t => t.Timestamp);
        builder.HasIndex(t => t.RepUserId);
        builder.HasIndex(t => t.JournalEntryId);
    }
}