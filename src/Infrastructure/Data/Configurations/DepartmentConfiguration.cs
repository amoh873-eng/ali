using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the Department entity (الأقسام).
/// Includes a self-referencing tree relationship like the Chart of Accounts.
/// </summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(d => d.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(d => d.NameAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.NameEn)
            .HasMaxLength(100);

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        // Self-referencing tree relationship
        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit fields
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
