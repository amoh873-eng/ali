using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the Lead entity (العملاء المحتملون).
/// </summary>
public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(l => l.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(l => l.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.NameEn).HasMaxLength(200);
        builder.Property(l => l.ContactPerson).HasMaxLength(200);
        builder.Property(l => l.Phone).HasMaxLength(30);
        builder.Property(l => l.Email).HasMaxLength(200);
        builder.Property(l => l.Notes).HasMaxLength(1000);

        builder.HasOne(l => l.ConvertedCustomer)
            .WithMany()
            .HasForeignKey(l => l.ConvertedCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
