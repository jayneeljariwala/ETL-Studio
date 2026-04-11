using ETL.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETL.Infrastructure.Persistence.Configurations;

public sealed class FieldMappingConfiguration : IEntityTypeConfiguration<FieldMapping>
{
    public void Configure(EntityTypeBuilder<FieldMapping> builder)
    {
        builder.ToTable("FieldMappings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SourceField)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.DestinationField)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.DefaultValue).HasMaxLength(500);

        builder.OwnsMany(x => x.TransformationSteps, owned =>
        {
            owned.ToTable("FieldMappingTransformationSteps");
            owned.WithOwner().HasForeignKey("FieldMappingId");

            owned.Property<int>("Id");
            owned.HasKey("Id");

            owned.Property(x => x.Type)
                .HasConversion<int>()
                .IsRequired();

            owned.Property(x => x.Parameter)
                .HasMaxLength(2000);

            owned.Property(x => x.Order).IsRequired();
        });
    }
}
