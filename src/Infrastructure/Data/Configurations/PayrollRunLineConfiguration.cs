using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// إعدادات Fluent API لكيان سطر دورة الرواتب (PayrollRunLine).
/// </summary>
public class PayrollRunLineConfiguration : IEntityTypeConfiguration<PayrollRunLine>
{
    public void Configure(EntityTypeBuilder<PayrollRunLine> builder)
    {
        builder.ToTable("PayrollRunLines");

        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.PayrollRun)
            .WithMany(p => p.Lines)
            .HasForeignKey(l => l.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Employee)
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // موظف واحد لا يظهر مرتين في نفس الدورة
        builder.HasIndex(l => new { l.PayrollRunId, l.EmployeeId })
            .IsUnique();

        builder.Property(l => l.BasicSalary).HasColumnType("decimal(18,2)").HasDefaultValue(0);
        builder.Property(l => l.UnpaidLeaveDeduction).HasColumnType("decimal(18,2)").HasDefaultValue(0);
        builder.Property(l => l.OtherDeductions).HasColumnType("decimal(18,2)").HasDefaultValue(0);
        builder.Property(l => l.OtherAllowances).HasColumnType("decimal(18,2)").HasDefaultValue(0);
        builder.Property(l => l.NetPay).HasColumnType("decimal(18,2)").HasDefaultValue(0);

        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
