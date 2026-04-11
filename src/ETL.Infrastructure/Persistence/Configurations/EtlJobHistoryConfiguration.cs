using ETL.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETL.Infrastructure.Persistence.Configurations;

public sealed class EtlJobHistoryConfiguration : IEntityTypeConfiguration<EtlJobHistory>
{
    public void Configure(EntityTypeBuilder<EtlJobHistory> builder)
    {
        builder.ToTable("ETLJobHistory");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.StartedAtUtc).IsRequired();
        builder.Property(x => x.CompletedAtUtc);
        builder.Property(x => x.RecordsRead).IsRequired();
        builder.Property(x => x.RecordsTransformed).IsRequired();
        builder.Property(x => x.RecordsLoaded).IsRequired();
        builder.Property(x => x.RecordsFailed).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc);
    }
}
