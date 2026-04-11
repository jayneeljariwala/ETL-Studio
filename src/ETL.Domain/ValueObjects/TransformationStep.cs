using ETL.Domain.Common;
using ETL.Domain.Enums;

namespace ETL.Domain.ValueObjects;

public sealed record TransformationStep
{
    public TransformationType Type { get; init; }
    public string? Parameter { get; init; }
    public int Order { get; init; }

    public static TransformationStep Create(TransformationType type, int order, string? parameter = null)
    {
        if (order < 0)
        {
            throw new DomainException("Transformation step order cannot be negative.");
        }

        if (type is TransformationType.DateFormat or TransformationType.CustomExpression
            && string.IsNullOrWhiteSpace(parameter))
        {
            throw new DomainException($"Transformation type '{type}' requires a parameter.");
        }

        return new TransformationStep
        {
            Type = type,
            Order = order,
            Parameter = parameter?.Trim()
        };
    }
}
