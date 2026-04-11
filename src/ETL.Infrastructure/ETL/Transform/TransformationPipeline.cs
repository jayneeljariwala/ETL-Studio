using ETL.Application.ETL.Abstractions;
using ETL.Application.ETL.Models;
using ETL.Domain.Enums;

namespace ETL.Infrastructure.ETL.Transform;

public sealed class TransformationPipeline : IDataTransformer
{
    private readonly ICustomExpressionEvaluator _expressionEvaluator;

    public TransformationPipeline(ICustomExpressionEvaluator expressionEvaluator)
    {
        _expressionEvaluator = expressionEvaluator;
    }

    public async Task<RecordTransformResult> TransformAsync(
        IReadOnlyDictionary<string, object?> sourceRecord,
        IReadOnlyCollection<FieldMappingDefinition> mappings,
        CancellationToken cancellationToken)
    {
        var destinationRecord = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings.OrderBy(x => x.Order))
        {
            sourceRecord.TryGetValue(mapping.SourceField, out var value);
            value = string.IsNullOrWhiteSpace(value?.ToString()) && mapping.DefaultValue is not null ? mapping.DefaultValue : value;

            foreach (var transformation in mapping.Transformations.OrderBy(x => x.Order))
            {
                value = await ApplyTransformationAsync(transformation, value, sourceRecord, cancellationToken);
            }

            if (mapping.IsRequired && string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return RecordTransformResult.Invalid(
                    $"Required field '{mapping.SourceField}' is missing for destination '{mapping.DestinationField}'.");
            }

            destinationRecord[mapping.DestinationField] = value;
        }

        return RecordTransformResult.Valid(destinationRecord);
    }

    private async Task<object?> ApplyTransformationAsync(
        TransformationDefinition transformation,
        object? value,
        IReadOnlyDictionary<string, object?> sourceRecord,
        CancellationToken cancellationToken)
    {
        return transformation.Type switch
        {
            TransformationType.Trim => value?.ToString()?.Trim(),
            TransformationType.Uppercase => value?.ToString()?.ToUpperInvariant(),
            TransformationType.Lowercase => value?.ToString()?.ToLowerInvariant(),
            TransformationType.DateFormat => FormatDate(value, transformation.Parameter),
            TransformationType.CustomExpression => await EvaluateExpressionAsync(transformation.Parameter, value, sourceRecord, cancellationToken),
            _ => value
        };
    }

    private static object? FormatDate(object? value, string? format)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            return value;
        }

        return DateTime.TryParse(value.ToString(), out var dateValue)
            ? dateValue.ToString(format)
            : value;
    }

    private async Task<object?> EvaluateExpressionAsync(
        string? expression,
        object? value,
        IReadOnlyDictionary<string, object?> sourceRecord,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return value;
        }

        return await _expressionEvaluator.EvaluateAsync(expression, value, sourceRecord, cancellationToken);
    }
}
