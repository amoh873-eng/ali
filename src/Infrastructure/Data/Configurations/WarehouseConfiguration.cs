using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the Warehouse entity (المخازن).
/// </summary>
public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(w => w.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(w => w.NameAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.NameEn)
            .HasMaxLength(100);

        builder.Property(w => w.Location)
            .HasMaxLength(200);

        // Audit fields
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.CreatedBy).HasMaxLength(100);
        builder.Property(w => w.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
