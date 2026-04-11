namespace ETL.Infrastructure.ETL.Transform;

public sealed class ScriptGlobals
{
    public object? Value { get; init; }
    public IReadOnlyDictionary<string, object?> Row { get; init; } = new Dictionary<string, object?>();
}
