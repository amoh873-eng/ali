using ERPSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

/// <summary>إعداد Fluent API لكيان إشعارات الموظفين (Notifications).</summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientUserId).IsRequired().HasMaxLength(450);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(300);
        builder.Property(n => n.Message).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(n => n.LinkUrl).HasMaxLength(1000);
        builder.Property(n => n.DedupKey).HasMaxLength(200);

        // مفتاح أجنبي إلى AspNetUsers (fallback إلى IdentityUser المدمج)
        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // فهارس: استعلامات المستلم والبحث عن مفتاح عدم التكرار
        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt });
        builder.HasIndex(n => n.DedupKey);

        builder.Property(n => n.CreatedAt).IsRequired();
    }
}