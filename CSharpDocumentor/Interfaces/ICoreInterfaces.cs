using CSharpDocumentor.Models;

namespace CSharpDocumentor.Interfaces
{
    public interface ILLMService
    {
        Task<List<DocumentationResponse>> GenerateDocumentationAsync(ClassContext context);
    }

    public interface IProjectScanner
    {
        Task<List<ClassContext>> ScanProjectAsync(string projectPath);
    }

    public interface IDocumentationInjector
    {
        Task InjectDocumentationAsync(string filePath, List<DocumentationResponse> documentation);
    }
}
