using System.Collections.Concurrent;
using ETL.Application.ETL.Abstractions;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace ETL.Infrastructure.ETL.Transform;

public sealed class RoslynExpressionEvaluator : ICustomExpressionEvaluator
{
    private static readonly ScriptOptions ScriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
        .AddReferences(
            typeof(object).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly)
        .AddImports("System", "System.Linq", "System.Collections.Generic");

    private static readonly ConcurrentDictionary<string, ScriptRunner<object?>> _cache = new();

    public Task<object?> EvaluateAsync(
        string expression,
        object? value,
        IReadOnlyDictionary<string, object?> row,
        CancellationToken cancellationToken)
    {
        var globals = new ScriptGlobals
        {
            Value = value,
            Row = row
        };

        var runner = _cache.GetOrAdd(expression, expr => 
            CSharpScript.Create<object?>(expr, ScriptOptions, typeof(ScriptGlobals)).CreateDelegate());

        return runner(globals, cancellationToken);
    }
}
