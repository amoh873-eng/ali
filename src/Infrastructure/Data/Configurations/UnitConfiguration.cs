using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the Unit entity (وحدات القياس).
/// </summary>
public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(u => u.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(u => u.NameAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.NameEn)
            .HasMaxLength(100);

        // Audit fields
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.CreatedBy).HasMaxLength(100);
        builder.Property(u => u.UpdatedBy).HasMaxLength(100);

        // Global query filter: exclude soft-deleted units automatically
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
