using System.ComponentModel.DataAnnotations;
using ETL.Domain.Enums;

namespace ETL.Web.Models.EtlJobs;

public sealed class FieldMappingInputViewModel
{
    [Required]
    [MaxLength(250)]
    public string SourceField { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string DestinationField { get; set; } = string.Empty;

    public int Order { get; set; }
    public bool IsRequired { get; set; }
    [MaxLength(500)]
    public string? DefaultValue { get; set; }
    public List<TransformationInputViewModel> Transformations { get; set; } = new();
}

public sealed class TransformationInputViewModel
{
    public TransformationType Type { get; set; }
    public string? Parameter { get; set; }
    public int Order { get; set; }
}
