using CSharpDocumentor.Interfaces;
using CSharpDocumentor.Models;
using System.Text.RegularExpressions;

namespace CSharpDocumentor.Services
{
    public class MockLLMService : ILLMService
    {
        public async Task<List<DocumentationResponse>> GenerateDocumentationAsync(ClassContext context)
        {
            Console.WriteLine($"[MockLLM] Calling simulated API for class: {context.ClassName} ({context.TargetMembers.Count} members)...");

            // Simulate network latency (2 seconds)
            await Task.Delay(2000);

            var responses = new List<DocumentationResponse>();

            foreach (var member in context.TargetMembers)
            {
                var xmlDoc = GenerateMockXml(member.MemberName, member.Signature);
                responses.Add(new DocumentationResponse
                {
                    MemberName = member.MemberName,
                    XmlDoc = xmlDoc
                });
            }

            Console.WriteLine($"[MockLLM] Simulated API response received for {context.ClassName}.");
            return responses;
        }

        private string GenerateMockXml(string name, string signature)
        {
            var summary = $"/// <summary>\n/// Simulated AI documentation for {name}.\n/// </summary>";
            var paramsDoc = string.Empty;
            var returnsDoc = string.Empty;

            // Simple regex to extract parameters for the mock
            var match = Regex.Match(signature, @"\((.*?)\)");
            if (match.Success)
            {
                var parameters = match.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var param in parameters)
                {
                    var paramName = param.Trim().Split(' ').Last();
                    paramsDoc += $"\n/// <param name=\"{paramName}\">The {paramName} parameter.</param>";
                }
            }

            if (!signature.Contains("void") && (signature.Contains("(") || !signature.Contains(" ")))
            {
                returnsDoc = "\n/// <returns>A simulated return value.</returns>";
            }

            return summary + paramsDoc + returnsDoc;
        }
    }
}
