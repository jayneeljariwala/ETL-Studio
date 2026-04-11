using System.ComponentModel.DataAnnotations;
using ETL.Domain.Enums;

namespace ETL.Web.Models.EtlJobs;

public sealed class EtlJobUpsertViewModel
{
    public Guid? Id { get; init; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DataSourceType SourceType { get; set; }

    [Required]
    public DataDestinationType DestinationType { get; set; }

    [Required]
    public LoadStrategy LoadStrategy { get; set; }

    [Required]
    public string SourceConfigurationJson { get; set; } = "{}";

    [Required]
    public string DestinationConfigurationJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public List<FieldMappingInputViewModel> FieldMappings { get; set; } = new();
}
