using Xunit;
using CSharpDocumentor.Services;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace CSharpDocumentor.Tests
{
    public class ProjectScannerTests
    {
        [Fact]
        public async Task ScanProjectAsync_ShouldIdentifyClassesWithUndocumentedMembers()
        {
            // Arrange
            var testProjectDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testProjectDir);
            var testFilePath = Path.Combine(testProjectDir, "TestClass.cs");

            var sourceCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void UndocumentedMethod() { }

        /// <summary>Already documented</summary>
        public void DocumentedMethod() { }
    }
}";
            await File.WriteAllTextAsync(testFilePath, sourceCode);

            var scanner = new ProjectScanner();

            // Act
            var result = await scanner.ScanProjectAsync(testProjectDir);

            // Assert
            Assert.NotEmpty(result);
            var classContext = result.First();
            Assert.Equal("TestClass", classContext.ClassName);
            Assert.Single(classContext.TargetMembers);
            Assert.Equal("UndocumentedMethod", classContext.TargetMembers.First().MemberName);

            // Cleanup
            Directory.Delete(testProjectDir, true);
        }
    }
}
