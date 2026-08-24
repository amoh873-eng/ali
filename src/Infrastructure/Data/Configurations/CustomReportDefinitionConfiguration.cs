using ERPSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Data.Configurations;

public class CustomReportDefinitionConfiguration : IEntityTypeConfiguration<CustomReportDefinition>
{
    public void Configure(EntityTypeBuilder<CustomReportDefinition> b)
    {
        b.ToTable("CustomReportDefinitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.ClientRequestNote).HasMaxLength(2000);
        b.Property(x => x.SqlQuery).HasColumnType("nvarchar(max)");
        b.Property(x => x.ExistingReportKey).HasMaxLength(100);
        b.Property(x => x.ParametersJson).HasColumnType("nvarchar(max)");
    }
}
