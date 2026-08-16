using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the StockMovement entity (حركات المخزون).
/// </summary>
public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Quantity)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(m => m.UnitCost)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(m => m.ReferenceNumber)
            .HasMaxLength(50);

        builder.Property(m => m.Note)
            .HasMaxLength(500);

        builder.Property(m => m.MovementDate)
            .IsRequired();

        // Relationships
        builder.HasOne(m => m.Item)
            .WithMany(i => i.StockMovements)
            .HasForeignKey(m => m.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Warehouse)
            .WithMany(w => w.StockMovements)
            .HasForeignKey(m => m.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for fast balance/history queries
        builder.HasIndex(m => new { m.ItemId, m.WarehouseId });
        builder.HasIndex(m => m.MovementDate);

        // Audit fields
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.CreatedBy).HasMaxLength(100);
        builder.Property(m => m.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
