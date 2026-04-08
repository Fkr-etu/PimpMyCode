using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDocumentor.Services
{
    public class ClassContextBuilder
    {
        private readonly int _maxBodyLines;

        public ClassContextBuilder(int maxBodyLines = 50)
        {
            _maxBodyLines = maxBodyLines;
        }

        public MethodDeclarationSyntax TruncateMethodBody(MethodDeclarationSyntax method)
        {
            if (method.Body == null) return method;

            var bodyText = method.Body.ToFullString();
            var lines = bodyText.Split(new[] { "\n" }, StringSplitOptions.None);

            if (lines.Length <= _maxBodyLines) return method;

            var truncatedComment = $"{{ // ... [body truncated, {lines.Length} lines] }}";
            var newBody = SyntaxFactory.Block(
                SyntaxFactory.ParseStatement(truncatedComment)
            );

            return method.WithBody(newBody);
        }
    }
}
