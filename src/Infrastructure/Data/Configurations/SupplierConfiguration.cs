using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the Supplier entity (الموردون).</summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code).IsRequired().HasMaxLength(20);
        builder.HasIndex(s => s.Code).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.Address).HasMaxLength(300);
        builder.Property(s => s.TaxNumber).HasMaxLength(50);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.CurrentBalance).HasColumnType("decimal(18,2)").HasDefaultValue(0);
        builder.Property(s => s.CreditLimit).HasColumnType("decimal(18,2)");

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}