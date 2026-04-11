using ETL.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETL.Infrastructure.Persistence.Configurations;

public sealed class EtlJobConfiguration : IEntityTypeConfiguration<EtlJob>
{
    public void Configure(EntityTypeBuilder<EtlJob> builder)
    {
        builder.ToTable("ETLJobs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.SourceType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.SourceConfigurationJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.DestinationType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.DestinationConfigurationJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.LoadStrategy)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CurrentStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc);

        builder.HasMany(x => x.FieldMappings)
            .WithOne(x => x.EtlJob)
            .HasForeignKey(x => x.EtlJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.JobHistory)
            .WithOne(x => x.EtlJob)
            .HasForeignKey(x => x.EtlJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
