using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpDocumentor.Interfaces;
using CSharpDocumentor.Models;

namespace CSharpDocumentor.Services
{
    public class ProjectScanner : IProjectScanner
    {
        public async Task<List<ClassContext>> ScanProjectAsync(string projectPath)
        {
            var results = new List<ClassContext>();
            var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

                foreach (var typeDeclaration in types)
                {
                    var undocumentedMembers = typeDeclaration.Members
                        .Where(m => m is MethodDeclarationSyntax or PropertyDeclarationSyntax or FieldDeclarationSyntax)
                        .Where(m => !HasXmlDocumentation(m))
                        .Select(m => new MemberDocumentationRequest
                        {
                            MemberName = GetMemberName(m),
                            Signature = m is MethodDeclarationSyntax meth ? meth.Identifier.Text + meth.ParameterList.ToString() : m.ToString()
                        })
                        .ToList();

                    if (undocumentedMembers.Any() || !HasXmlDocumentation(typeDeclaration))
                    {
                        var ns = typeDeclaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? string.Empty;

                        results.Add(new ClassContext
                        {
                            ClassName = typeDeclaration.Identifier.Text,
                            FullSource = typeDeclaration.ToString(),
                            Namespace = ns,
                            TargetMembers = undocumentedMembers,
                            ProjectName = Path.GetFileName(projectPath),
                            FilePath = file
                        });
                    }
                }
            }

            return results;
        }

        private bool HasXmlDocumentation(SyntaxNode node)
        {
            var trivia = node.GetLeadingTrivia();
            return trivia.Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                   t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        }

        private string GetMemberName(SyntaxNode node)
        {
            return node switch
            {
                MethodDeclarationSyntax m => m.Identifier.Text,
                PropertyDeclarationSyntax p => p.Identifier.Text,
                FieldDeclarationSyntax f => f.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "Unknown",
                _ => "Unknown"
            };
        }
    }
}
