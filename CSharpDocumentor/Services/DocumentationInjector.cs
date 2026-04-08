using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpDocumentor.Interfaces;
using CSharpDocumentor.Models;

namespace CSharpDocumentor.Services
{
    public class DocumentationInjector : IDocumentationInjector
    {
        public async Task InjectDocumentationAsync(string filePath, List<DocumentationResponse> documentation)
        {
            var code = await File.ReadAllTextAsync(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();

            var rewriter = new DocumentationRewriter(documentation);
            var newRoot = rewriter.Visit(root);

            if (newRoot != root)
            {
                await File.WriteAllTextAsync(filePath, newRoot.ToFullString());
            }
        }
    }

    internal class DocumentationRewriter : CSharpSyntaxRewriter
    {
        private readonly List<DocumentationResponse> _documentation;

        public DocumentationRewriter(List<DocumentationResponse> documentation)
        {
            _documentation = documentation;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var updatedNode = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
            return InjectIfMatch(updatedNode, updatedNode.Identifier.Text);
        }

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var updatedNode = (InterfaceDeclarationSyntax)base.VisitInterfaceDeclaration(node)!;
            return InjectIfMatch(updatedNode, updatedNode.Identifier.Text);
        }

        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            var updatedNode = (RecordDeclarationSyntax)base.VisitRecordDeclaration(node)!;
            return InjectIfMatch(updatedNode, updatedNode.Identifier.Text);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return InjectIfMatch(node, node.Identifier.Text);
        }

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            return InjectIfMatch(node, node.Identifier.Text);
        }

        public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var firstVar = node.Declaration.Variables.FirstOrDefault();
            if (firstVar != null)
            {
                return InjectIfMatch(node, firstVar.Identifier.Text);
            }
            return base.VisitFieldDeclaration(node);
        }

        private SyntaxNode InjectIfMatch(SyntaxNode node, string name)
        {
            var doc = _documentation.FirstOrDefault(d => d.MemberName == name);
            if (doc != null)
            {
                var xmlTrivia = SyntaxFactory.ParseLeadingTrivia(doc.XmlDoc + "\n");
                var existingTrivia = node.GetLeadingTrivia();

                if (existingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)))
                    return node;

                return node.WithLeadingTrivia(existingTrivia.InsertRange(0, xmlTrivia));
            }
            return node;
        }
    }
}
