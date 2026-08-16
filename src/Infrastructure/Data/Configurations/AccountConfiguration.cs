using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the Account entity.
/// Using Fluent API instead of Data Annotations for two reasons:
/// 1. Keeps the Domain layer clean (no EF Core dependencies)
/// 2. Provides more control over database-specific settings
/// </summary>
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Account> builder)
    {
        // Table name
        builder.ToTable("Accounts");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Code: unique and indexed for fast lookup
        builder.Property(a => a.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(a => a.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0"); // Soft-deleted accounts don't block unique codes

        // NameAr: required, Arabic name is mandatory
        builder.Property(a => a.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        // NameEn: optional
        builder.Property(a => a.NameEn)
            .HasMaxLength(200);

        // Description: optional, max 500 characters
        builder.Property(a => a.Description)
            .HasMaxLength(500);

        // Self-referencing relationship for tree structure
        // ParentAccountId is nullable (root accounts have no parent)
        // DeleteBehavior.Restrict prevents cascade delete - we don't want to
        // accidentally delete all children when a parent is deleted
        builder.HasOne(a => a.ParentAccount)
            .WithMany(a => a.ChildAccounts)
            .HasForeignKey(a => a.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // CurrentBalance: decimal with 18 digits total, 2 decimal places
        // Sufficient for most currencies up to quadrillions
        builder.Property(a => a.CurrentBalance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        // Audit fields
        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.CreatedBy)
            .HasMaxLength(100);

        builder.Property(a => a.UpdatedBy)
            .HasMaxLength(100);

        // Global query filter: automatically exclude soft-deleted records
        // This means we never accidentally query deleted accounts
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
