using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the SalesQuote entity (عروض الأسعار).
/// </summary>
public class SalesQuoteConfiguration : IEntityTypeConfiguration<SalesQuote>
{
    public void Configure(EntityTypeBuilder<SalesQuote> builder)
    {
        builder.ToTable("SalesQuotes");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuoteNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(q => q.QuoteNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(q => new { q.CustomerId, q.QuoteDate });

        // Financial fields: 18 digits total, 2 decimals
        builder.Property(q => q.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(q => q.DiscountPercentage).HasColumnType("decimal(18,2)");
        builder.Property(q => q.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(q => q.TaxRate).HasColumnType("decimal(18,2)");
        builder.Property(q => q.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(q => q.TotalAmount).HasColumnType("decimal(18,2)");

        builder.Property(q => q.Note).HasMaxLength(1000);
        builder.Property(q => q.QuoteDate).IsRequired();

        builder.HasOne(q => q.Customer)
            .WithMany()
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lines cascade-delete with their quote
        builder.HasMany(q => q.Lines)
            .WithOne(l => l.SalesQuote)
            .HasForeignKey(l => l.SalesQuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(q => q.CreatedAt).IsRequired();
        builder.Property(q => q.CreatedBy).HasMaxLength(100);
        builder.Property(q => q.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(q => !q.IsDeleted);
    }
}