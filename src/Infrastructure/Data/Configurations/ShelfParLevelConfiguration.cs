using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لحد الرف المستهدَف.</summary>
public class ShelfParLevelConfiguration : IEntityTypeConfiguration<ShelfParLevel>
{
    public void Configure(EntityTypeBuilder<ShelfParLevel> builder)
    {
        builder.ToTable("ShelfParLevels");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ItemId).IsRequired();
        builder.Property(p => p.Location).IsRequired().HasMaxLength(120);
        builder.Property(p => p.ParLevel).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.HasIndex(p => p.ItemId);
        builder.HasIndex(p => p.Location);
    }
}