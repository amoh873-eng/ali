using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لفحص الرفوف.</summary>
public class ShelfCheckConfiguration : IEntityTypeConfiguration<ShelfCheck>
{
    public void Configure(EntityTypeBuilder<ShelfCheck> builder)
    {
        builder.ToTable("ShelfChecks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ItemId).IsRequired();
        builder.Property(c => c.Location).IsRequired().HasMaxLength(120);
        builder.Property(c => c.ObservedQty).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(c => c.ParLevel).HasColumnType("numeric(18,2)");
        builder.Property(c => c.PhotoOnePath).HasMaxLength(300);
        builder.Property(c => c.PhotoTwoPath).HasMaxLength(300);
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.Property(c => c.CreatedByUserId).IsRequired().HasMaxLength(450);
        builder.Property(c => c.CheckedAt).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => c.ItemId);
        builder.HasIndex(c => c.Location);
        builder.HasIndex(c => c.CheckedAt);
    }
}