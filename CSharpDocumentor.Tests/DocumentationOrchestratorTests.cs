using Moq;
using Xunit;
using CSharpDocumentor.Interfaces;
using CSharpDocumentor.Models;
using CSharpDocumentor.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CSharpDocumentor.Tests
{
    public class DocumentationOrchestratorTests
    {
        [Fact]
        public async Task RunAsync_ShouldOrchestrateFullWorkflow()
        {
            // Arrange
            var projectPath = "C:/TestProject";
            var mockScanner = new Mock<IProjectScanner>();
            var mockLLM = new Mock<ILLMService>();
            var mockInjector = new Mock<IDocumentationInjector>();

            var classContexts = new List<ClassContext>
            {
                new ClassContext
                {
                    ClassName = "TestClass",
                    FilePath = "C:/TestProject/TestClass.cs",
                    TargetMembers = new List<MemberDocumentationRequest> { new MemberDocumentationRequest { MemberName = "Method1" } }
                }
            };

            var llmResponses = new List<DocumentationResponse>
            {
                new DocumentationResponse { MemberName = "Method1", XmlDoc = "/// <summary>Test</summary>" }
            };

            mockScanner.Setup(s => s.ScanProjectAsync(projectPath)).ReturnsAsync(classContexts);
            mockLLM.Setup(l => l.GenerateDocumentationAsync(It.IsAny<ClassContext>())).ReturnsAsync(llmResponses);

            var orchestrator = new DocumentationOrchestrator(mockScanner.Object, mockLLM.Object, mockInjector.Object);

            // Act
            await orchestrator.RunAsync(projectPath);

            // Assert
            mockScanner.Verify(s => s.ScanProjectAsync(projectPath), Times.Once);
            mockLLM.Verify(l => l.GenerateDocumentationAsync(It.IsAny<ClassContext>()), Times.Exactly(classContexts.Count));
            mockInjector.Verify(i => i.InjectDocumentationAsync("C:/TestProject/TestClass.cs", llmResponses), Times.Once);
        }
    }
}
