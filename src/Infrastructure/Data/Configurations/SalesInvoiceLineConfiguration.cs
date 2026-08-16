using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the SalesInvoiceLine entity (بنود فواتير المبيعات).
/// </summary>
public class SalesInvoiceLineConfiguration : IEntityTypeConfiguration<SalesInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> builder)
    {
        builder.ToTable("SalesInvoiceLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(l => l.UnitCost).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(l => l.LineTotal).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.SalesInvoiceId);
        builder.HasIndex(l => l.ItemId);

        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
