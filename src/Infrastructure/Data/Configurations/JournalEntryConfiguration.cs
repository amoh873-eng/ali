using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for the JournalEntry entity (القيود المحاسبية).
/// </summary>
public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.EntryNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(e => e.EntryDate);
        builder.HasIndex(e => e.ReferenceNumber);

        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.EntryDate).IsRequired();

        // Lines cascade-delete with their parent entry
        builder.HasMany(e => e.Lines)
            .WithOne(l => l.JournalEntry)
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
