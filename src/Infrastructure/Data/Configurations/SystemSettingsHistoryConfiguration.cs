using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

public class SystemSettingsHistoryConfiguration : IEntityTypeConfiguration<SystemSettingsHistory>
{
    public void Configure(EntityTypeBuilder<SystemSettingsHistory> b)
    {
        b.ToTable("SystemSettingsHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.SnapshotJson).IsRequired().HasColumnType("text");
        b.Property(x => x.ChangedBy).HasMaxLength(200);
        b.Property(x => x.ChangeNote).HasMaxLength(500);
        b.HasIndex(x => x.ChangedAt);
    }
}
