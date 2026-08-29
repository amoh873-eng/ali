using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LogoUrl).HasMaxLength(500);
        builder.Property(e => e.AppDisplayName).HasMaxLength(200);
        builder.Property(e => e.PrimaryColorHex).HasMaxLength(20);
        builder.Property(e => e.LicensedToClientName).IsRequired().HasMaxLength(300);
        builder.Property(e => e.SupportContactInfo).HasMaxLength(1000);
        builder.Property(e => e.FeatureFlagsJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(e => e.BackupDestination).HasMaxLength(500);
        builder.Property(e => e.TrialStartedAt).HasColumnType("datetime2");
        builder.Property(e => e.TrialExpiresAt).HasColumnType("datetime2");

        builder.Property(e => e.ExpiryWarningWindowDays).HasDefaultValue(7);
        builder.Property(e => e.WeightBarcodeRuleJson).HasColumnType("nvarchar(max)");
    }
}
