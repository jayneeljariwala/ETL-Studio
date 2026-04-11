using ETL.Domain.Enums;

namespace ETL.Application.ETL.Models;

public sealed class TransformationDefinition
{
    public TransformationType Type { get; init; }
    public string? Parameter { get; init; }
    public int Order { get; init; }
}
