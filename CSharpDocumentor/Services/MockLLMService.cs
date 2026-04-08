using CSharpDocumentor.Interfaces;
using CSharpDocumentor.Models;

namespace CSharpDocumentor.Services
{
    public class MockLLMService : ILLMService
    {
        public Task<List<DocumentationResponse>> GenerateDocumentationAsync(ClassContext context)
        {
            var responses = new List<DocumentationResponse>();

            foreach (var member in context.TargetMembers)
            {
                responses.Add(new DocumentationResponse
                {
                    MemberName = member.MemberName,
                    XmlDoc = $"/// <summary>\n/// Mock documentation for {member.MemberName}.\n/// </summary>"
                });
            }

            return Task.FromResult(responses);
        }
    }
}
