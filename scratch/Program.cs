using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;

public class ScriptGlobals
{
    public object? Value { get; set; }
    public IReadOnlyDictionary<string, object?> Row { get; set; } = null!;
}

class Program
{
    static async System.Threading.Tasks.Task Main()
    {
        var options = ScriptOptions.Default
            .AddReferences(
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(List<>).Assembly)
            .AddImports("System", "System.Linq", "System.Collections.Generic");

        string scriptCode = "\"str\".";
        int position = 6;  

        var script = CSharpScript.Create<object?>(scriptCode, options, typeof(ScriptGlobals));
        var compilation = script.GetCompilation();
        var syntaxTree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var root = await syntaxTree.GetRootAsync();
        var token = root.FindToken(position);

        Console.WriteLine($"Token: {token.Text}, Kind: {token.Kind()}");
        Console.WriteLine($"Token Parent: {token.Parent?.GetType().Name}");

        ITypeSymbol? typeSymbol = null;
        var node = token.Parent;
        while (node != null)
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
                typeSymbol = typeInfo.Type;
                Console.WriteLine($"Expression: {memberAccess.Expression}, Type: {typeInfo.Type?.Name}");
                break;
            }
            if (node is ConditionalAccessExpressionSyntax condAccess)
            {
                var typeInfo = semanticModel.GetTypeInfo(condAccess.Expression);
                typeSymbol = typeInfo.Type;
                Console.WriteLine($"Expression: {condAccess.Expression}, Type: {typeInfo.Type?.Name}");
                break;
            }
            node = node.Parent;
        }

        if (typeSymbol != null) {
            Console.WriteLine($"TypeSymbol found: {typeSymbol.Name}");
        } else {
            Console.WriteLine("TypeSymbol NOT found.");
        }
    }
}
