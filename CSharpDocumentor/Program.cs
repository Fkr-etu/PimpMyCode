using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CSharpDocumentor.Interfaces;
using CSharpDocumentor.Services;

namespace CSharpDocumentor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet run -- /path/to/project");
                return;
            }

            var projectPath = args[0];

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, configuration);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                var orchestrator = serviceProvider.GetRequiredService<DocumentationOrchestrator>();
                Console.WriteLine($"Starting documentation for project at: {projectPath}");
                await orchestrator.RunAsync(projectPath);
                Console.WriteLine("Documentation completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IProjectScanner, ProjectScanner>();
            services.AddSingleton<IDocumentationInjector, DocumentationInjector>();

            // Flexible LLM Service injection
            var provider = configuration["LLMConfig:Provider"];
            if (provider == "Mock")
            {
                services.AddSingleton<ILLMService, MockLLMService>();
            }
            // else if (provider == "Claude") { services.AddSingleton<ILLMService, ClaudeLLMService>(); }

            services.AddSingleton<DocumentationOrchestrator>();
        }
    }
}
