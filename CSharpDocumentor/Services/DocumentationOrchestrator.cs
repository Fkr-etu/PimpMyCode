using CSharpDocumentor.Interfaces;
using CSharpDocumentor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpDocumentor.Services
{
    public class DocumentationOrchestrator
    {
        private readonly IProjectScanner _scanner;
        private readonly ILLMService _llmService;
        private readonly IDocumentationInjector _injector;
        private readonly ClassContextBuilder _contextBuilder;

        public DocumentationOrchestrator(IProjectScanner scanner, ILLMService llmService, IDocumentationInjector injector)
        {
            _scanner = scanner;
            _llmService = llmService;
            _injector = injector;
            _contextBuilder = new ClassContextBuilder();
        }

        public async Task RunAsync(string projectPath)
        {
            var classes = await _scanner.ScanProjectAsync(projectPath);

            foreach (var @class in classes)
            {
                var preparedContext = PrepareContext(@class);
                var documentation = await _llmService.GenerateDocumentationAsync(preparedContext);
                await _injector.InjectDocumentationAsync(@class.FilePath, documentation);
            }
        }

        private ClassContext PrepareContext(ClassContext original)
        {
            var tree = CSharpSyntaxTree.ParseText(original.FullSource);
            var root = tree.GetRoot();

            var rewrittenRoot = new MethodBodyTruncator(_contextBuilder).Visit(root);

            return new ClassContext
            {
                ClassName = original.ClassName,
                FullSource = rewrittenRoot.ToFullString(),
                Namespace = original.Namespace,
                TargetMembers = original.TargetMembers,
                ProjectName = original.ProjectName,
                FilePath = original.FilePath
            };
        }

        private class MethodBodyTruncator : CSharpSyntaxRewriter
        {
            private readonly ClassContextBuilder _builder;
            public MethodBodyTruncator(ClassContextBuilder builder) => _builder = builder;

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                return _builder.TruncateMethodBody(node);
            }
        }
    }
}
