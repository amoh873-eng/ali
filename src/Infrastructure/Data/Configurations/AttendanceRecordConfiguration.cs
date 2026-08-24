using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// إعدادات Fluent API لكيان سجل الحضور (AttendanceRecord).
/// </summary>
public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");

        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // تحويل DateOnly إلى date في SQL Server
        builder.Property(a => a.Date)
            .HasConversion(
                d => d.ToDateTime(TimeOnly.MinValue),
                d => DateOnly.FromDateTime(d))
            .HasColumnType("date")
            .IsRequired();

        // فهرس فريد: موظف واحد لا يمكن أن يملك أكثر من سجل واحد لنفس اليوم
        // الفلتر يستثني السجلات المحذوفة ناعماً حتى لا تمنع إعادة الإنشاء
        builder.HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(a => a.Notes).HasMaxLength(500);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
