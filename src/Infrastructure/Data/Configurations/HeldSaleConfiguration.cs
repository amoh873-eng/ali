using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لكيان عمليات البيع الموقوفة (HeldSales).</summary>
public class HeldSaleConfiguration : IEntityTypeConfiguration<HeldSale>
{
    public void Configure(EntityTypeBuilder<HeldSale> builder)
    {
        builder.ToTable("HeldSales");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.CashierUserId).IsRequired().HasMaxLength(450);
        builder.Property(h => h.LinesJson).IsRequired().HasColumnType("text");
        builder.Property(h => h.Note).HasMaxLength(500);
        builder.Property(h => h.CreatedAt).IsRequired();

        // العميل اختياري (قد يكون زبون نقدي)
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(h => h.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        // فهرس لاستعلامات أمين الصندوق
        builder.HasIndex(h => h.CashierUserId);
    }
}