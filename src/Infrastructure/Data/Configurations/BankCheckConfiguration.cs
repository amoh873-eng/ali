using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لكيان الشيكات البنكية (BankCheck).</summary>
public class BankCheckConfiguration : IEntityTypeConfiguration<BankCheck>
{
    public void Configure(EntityTypeBuilder<BankCheck> builder)
    {
        builder.ToTable("BankChecks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.CheckNumber).IsRequired().HasMaxLength(100);
        builder.Property(t => t.BankName).IsRequired().HasMaxLength(150);
        builder.Property(t => t.BranchName).HasMaxLength(150);
        builder.Property(t => t.Direction).IsRequired();
        builder.Property(t => t.Amount).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(t => t.IssueDate).IsRequired();
        builder.Property(t => t.DueDate).IsRequired();
        builder.Property(t => t.ReceivedOrIssuedDate).IsRequired();
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // الشيك مرتبط بعميل (استلام) أو بمورد (إصدار) — حسب الاتجاه
        builder.HasOne(t => t.Customer)
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Supplier)
            .WithMany()
            .HasForeignKey(t => t.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // القيد المحاسبي المرتبط بآخر حدث (فتح/تحصيل/ارتداد)
        builder.HasOne(t => t.JournalEntry)
            .WithMany()
            .HasForeignKey(t => t.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // فهارس محرك التقرير + المتابعة
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Direction);
        builder.HasIndex(t => t.CustomerId);
        builder.HasIndex(t => t.SupplierId);
        builder.HasIndex(t => t.JournalEntryId);
    }
}