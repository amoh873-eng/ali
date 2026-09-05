using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the ExpenseCategory entity (فئات المصاريف).
/// Links each category to an expense account in the Chart of Accounts.
/// </summary>
public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(c => c.NameAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.NameEn)
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        // Each category is linked to one expense account.
        builder.HasOne(c => c.Account)
            .WithMany()
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit fields
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
