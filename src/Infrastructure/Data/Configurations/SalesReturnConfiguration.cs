using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the SalesReturn entity (مردودات المبيعات).
/// </summary>
public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("SalesReturns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReturnNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(r => r.ReturnNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(r => new { r.CustomerId, r.ReturnDate });

        builder.Property(r => r.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(r => r.DiscountPercentage).HasColumnType("decimal(18,2)");
        builder.Property(r => r.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.TaxRate).HasColumnType("decimal(18,2)");
        builder.Property(r => r.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.TotalAmount).HasColumnType("decimal(18,2)");

        builder.Property(r => r.Note).HasMaxLength(1000);
        builder.Property(r => r.ReturnDate).IsRequired();

        // Relationships
        builder.HasOne(r => r.SalesInvoice)
            .WithMany(i => i.SalesReturns)
            .HasForeignKey(r => r.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Customer)
            .WithMany(c => c.SalesReturns)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.SalesReturn)
            .HasForeignKey(l => l.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(r => r.SalesJournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(r => r.CogsJournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
