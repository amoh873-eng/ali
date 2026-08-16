using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the ExpenseEntry entity (سندات المصروف).
/// </summary>
public class ExpenseEntryConfiguration : IEntityTypeConfiguration<ExpenseEntry>
{
    public void Configure(EntityTypeBuilder<ExpenseEntry> builder)
    {
        builder.ToTable("ExpenseEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(e => e.EntryNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.AttachmentPath)
            .HasMaxLength(500);

        // Each voucher belongs to one expense category.
        builder.HasOne(e => e.ExpenseCategory)
            .WithMany(c => c.ExpenseEntries)
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
