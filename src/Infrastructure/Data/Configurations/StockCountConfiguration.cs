using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the StockCount entity (الجرد الدوري).
/// </summary>
public class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("StockCounts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.StockCountNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.StockCountNumber).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.Property(c => c.CountDate).IsRequired();
        builder.Property(c => c.Note).HasMaxLength(1000);

        builder.HasOne(c => c.Warehouse)
            .WithMany()
            .HasForeignKey(c => c.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Lines)
            .WithOne(l => l.StockCount)
            .HasForeignKey(l => l.StockCountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}