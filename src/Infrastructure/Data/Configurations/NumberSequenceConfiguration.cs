using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the NumberSequence entity (العدّادات التسلسلية).
/// </summary>
public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable("NumberSequences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SequenceKey)
            .IsRequired()
            .HasMaxLength(100);

        // فهرس فريد على المفتاح لضمان عدم وجود عدّادين لنفس المفتاح
        builder.HasIndex(e => e.SequenceKey)
            .IsUnique();

        builder.Property(e => e.LastValue).IsRequired();
    }
}
