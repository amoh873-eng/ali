using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// إعدادات Fluent API لكيان دورة الرواتب (PayrollRun).
/// </summary>
public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("PayrollRuns");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.RunNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(p => p.RunNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // قيد فريد على الفترة — خط الدفاع الثاني (الأول: فحص مقفل داخل معاملة)
        builder.HasIndex(p => new { p.PeriodStart, p.PeriodEnd })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(p => p.PeriodStart).IsRequired();
        builder.Property(p => p.PeriodEnd).IsRequired();

        builder.Property(p => p.TotalGross).HasColumnType("decimal(18,2)").HasDefaultValue(0);
        builder.Property(p => p.TotalDeductions).HasColumnType("decimal(18,2)").HasDefaultValue(0);
        builder.Property(p => p.TotalNet).HasColumnType("decimal(18,2)").HasDefaultValue(0);

        builder.HasOne(p => p.JournalEntry)
            .WithMany()
            .HasForeignKey(p => p.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // RowVersion: رمز التزامن المتفائل
        builder.Property(p => p.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.PayrollRun)
            .HasForeignKey(l => l.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
