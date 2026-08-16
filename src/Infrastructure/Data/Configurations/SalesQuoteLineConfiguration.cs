using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the SalesQuoteLine entity (بنود عروض الأسعار).
/// </summary>
public class SalesQuoteLineConfiguration : IEntityTypeConfiguration<SalesQuoteLine>
{
    public void Configure(EntityTypeBuilder<SalesQuoteLine> builder)
    {
        builder.ToTable("SalesQuoteLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(l => l.LineTotal).HasColumnType("decimal(18,2)");

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}