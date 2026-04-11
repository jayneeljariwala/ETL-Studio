namespace ETL.Application.ETL.Models;

public sealed class RecordTransformResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
    public IReadOnlyDictionary<string, object?> Record { get; init; } = new Dictionary<string, object?>();

    public static RecordTransformResult Valid(IReadOnlyDictionary<string, object?> record) =>
        new()
        {
            IsValid = true,
            Record = record
        };

    public static RecordTransformResult Invalid(string error) =>
        new()
        {
            IsValid = false,
            Error = error
        };
}
