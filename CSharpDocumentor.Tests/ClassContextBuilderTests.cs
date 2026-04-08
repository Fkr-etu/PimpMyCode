using Xunit;
using CSharpDocumentor.Services;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace CSharpDocumentor.Tests
{
    public class ClassContextBuilderTests
    {
        [Fact]
        public void TruncateMethodBody_ShouldReplaceLongBodyWithComment()
        {
            // Arrange
            var source = @"
    public class Dummy {
        public void LongMethod()
        {
            // Line 1
            // Line 2
            // Line 3
            // Line 4
            // Line 5
            // Line 6
        }
    }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetRoot();
            var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();

            var builder = new ClassContextBuilder(maxBodyLines: 3);

            // Act
            var result = builder.TruncateMethodBody(method);

            // Assert
            var resultText = result.ToString();
            Assert.Contains("// ... [body truncated", resultText);
            Assert.DoesNotContain("// Line 4", resultText);
        }
    }
}
