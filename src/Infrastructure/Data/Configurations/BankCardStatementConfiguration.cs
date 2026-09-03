using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the BankCardStatement entity (صفوف كشف البنك للبطاقات).
/// </summary>
public class BankCardStatementConfiguration : IEntityTypeConfiguration<BankCardStatement>
{
    public void Configure(EntityTypeBuilder<BankCardStatement> builder)
    {
        builder.ToTable("BankCardStatements");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Reference)
            .IsRequired()
            .HasMaxLength(60);

        // مفتاح فريد: رقم المرجع فريد كفاية على كشف البنك، ونمنع تكراره عند إعادة الاستيراد
        builder.HasIndex(b => b.Reference)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(b => b.Amount).HasColumnType("decimal(18,2)");
        builder.Property(b => b.TransactionDate).IsRequired();
        builder.Property(b => b.ImportedAt).IsRequired();
        builder.Property(b => b.ImportedBy).HasMaxLength(100);
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();
        builder.Property(b => b.CreatedBy).HasMaxLength(100);
        builder.Property(b => b.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}