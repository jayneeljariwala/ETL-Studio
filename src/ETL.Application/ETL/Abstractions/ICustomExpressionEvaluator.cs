namespace ETL.Application.ETL.Abstractions;

public interface ICustomExpressionEvaluator
{
    Task<object?> EvaluateAsync(
        string expression,
        object? value,
        IReadOnlyDictionary<string, object?> row,
        CancellationToken cancellationToken);
}
