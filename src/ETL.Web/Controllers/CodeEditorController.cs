using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Linq;
using System.Threading.Tasks;

namespace ETL.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeEditorController : ControllerBase
{
    public class CompletionRequest
    {
        public string Code { get; set; } = string.Empty;
        public int Position { get; set; }
    }

    public class ScriptGlobals
    {
        public object? Value { get; set; }
        public System.Collections.Generic.IReadOnlyDictionary<string, object?> Row { get; set; } = null!;
    }

    [HttpPost("completions")]
    public async Task<IActionResult> GetCompletions([FromBody] CompletionRequest request)
    {
        try
        {
            var options = ScriptOptions.Default
                .AddReferences(
                    typeof(object).Assembly,
                    typeof(Enumerable).Assembly,
                    typeof(System.Collections.Generic.List<>).Assembly)
                .AddImports("System", "System.Linq", "System.Collections.Generic");

            // Fix cursor position offset because Monaco position is 1-based, but we get an index.
            // Wait, the frontend will send a 0-based offset. Let's assume request.Position is a 0-based character index.
            var scriptCode = request.Code;
            var position = request.Position;
            if (position > 0) position--;

            var script = CSharpScript.Create<object?>(scriptCode, options, typeof(ScriptGlobals));
            var compilation = script.GetCompilation();
            var syntaxTree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var root = await syntaxTree.GetRootAsync();
            var token = root.FindToken(position);
            
            if (token.IsKind(SyntaxKind.EndOfFileToken) && position > 0)
            {
                token = root.FindToken(position - 1);
            }

            ITypeSymbol? typeSymbol = null;
            var node = token.Parent;
            while (node != null)
            {
                if (node is MemberAccessExpressionSyntax memberAccess)
                {
                    typeSymbol = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
                    break;
                }
                if (node is ConditionalAccessExpressionSyntax condAccess)
                {
                    typeSymbol = semanticModel.GetTypeInfo(condAccess.Expression).Type;
                    break;
                }
                node = node.Parent;
            }

            if (typeSymbol != null)
            {
                var members = typeSymbol.GetMembers()
                    .Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsImplicitlyDeclared)
                    .GroupBy(m => m.Name)
                    .Select(g => g.First())
                    .ToList();

                var suggestions = members.Select(m => new {
                    label = m.Name,
                    // CompletionItemKind.Method = 1, Property = 9
                    kind = m is IMethodSymbol ? 1 : 9, 
                    insertText = m is IMethodSymbol ? m.Name + "()" : m.Name,
                    detail = m.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                });

                return Ok(new { suggestions });
            }

            // Global context completions (when not typing a dot)
            return Ok(new { suggestions = new[] {
                new { label = "Value", kind = 4, insertText = "Value", detail = "Current field value (object)" },
                new { label = "Row", kind = 4, insertText = "Row", detail = "Row fields dictionary" },
                new { label = "Math", kind = 9, insertText = "Math", detail = "System.Math" },
                new { label = "Convert", kind = 9, insertText = "Convert", detail = "System.Convert" },
            }});
        }
        catch (System.Exception)
        {
            return Ok(new { suggestions = Array.Empty<object>() });
        }
    }
}
