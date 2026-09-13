using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لرصد أسعار المنافسين.</summary>
public class ShelfPriceCaptureConfiguration : IEntityTypeConfiguration<ShelfPriceCapture>
{
    public void Configure(EntityTypeBuilder<ShelfPriceCapture> builder)
    {
        builder.ToTable("ShelfPriceCaptures");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ItemId).IsRequired();
        builder.Property(c => c.CompetitorName).IsRequired().HasMaxLength(120);
        builder.Property(c => c.Price).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(c => c.PhotoPath).HasMaxLength(300);
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.Property(c => c.CreatedByUserId).IsRequired().HasMaxLength(450);
        builder.Property(c => c.CapturedAt).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.HasIndex(c => c.ItemId);
        builder.HasIndex(c => c.CapturedAt);
    }
}