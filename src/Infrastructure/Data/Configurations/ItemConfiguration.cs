using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the Item entity (الأصناف).
/// </summary>
public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(i => i.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(i => i.Barcode)
            .IsUnique(false)
            .HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");

        builder.Property(i => i.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.NameEn)
            .HasMaxLength(200);

        builder.Property(i => i.Description)
            .HasMaxLength(500);

        builder.Property(i => i.Barcode)
            .HasMaxLength(50);

        // Financial fields: 18 digits total, 2 decimal places
        builder.Property(i => i.CostPrice)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(i => i.SalePrice)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(i => i.MinStockLevel)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(i => i.MaxStockLevel)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(i => i.CurrentStock)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        // ── تتبّع انتهاء الصلاحية: اختياري حسب الصنف، لا يغيّر سلوك الأصناف الأخرى ──
        builder.Property(i => i.TracksExpiry)
            .HasDefaultValue(false);

        builder.Property(i => i.LastLowStockNotifiedAt)
            .HasColumnType("datetime2");

        // Relationships
        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Unit)
            .WithMany(u => u.Items)
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit fields
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.CreatedBy).HasMaxLength(100);
        builder.Property(i => i.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
