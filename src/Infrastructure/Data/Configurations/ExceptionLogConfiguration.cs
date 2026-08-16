using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the ExceptionLog entity (سجل الأخطاء).
/// </summary>
public class ExceptionLogConfiguration : IEntityTypeConfiguration<ExceptionLog>
{
    public void Configure(EntityTypeBuilder<ExceptionLog> builder)
    {
        builder.ToTable("ExceptionLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.Message).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.StackTrace).HasColumnType("nvarchar(max)");
        builder.Property(e => e.UserId).HasMaxLength(450);
        builder.Property(e => e.RequestPath).HasMaxLength(1000);
        builder.Property(e => e.ExceptionType).HasMaxLength(500);

        // فهارس لتسريع الفلترة والترتيب (الأحدث أولاً)
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.ExceptionType);
    }
}
