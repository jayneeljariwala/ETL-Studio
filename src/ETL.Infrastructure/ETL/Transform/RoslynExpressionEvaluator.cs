using ETL.Application.ETL.Abstractions;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace ETL.Infrastructure.ETL.Transform;

public sealed class RoslynExpressionEvaluator : ICustomExpressionEvaluator
{
    private static readonly ScriptOptions ScriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
        .AddImports("System", "System.Linq", "System.Collections.Generic");

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

        return CSharpScript.EvaluateAsync<object?>(expression, ScriptOptions, globals, cancellationToken: cancellationToken);
    }
}
