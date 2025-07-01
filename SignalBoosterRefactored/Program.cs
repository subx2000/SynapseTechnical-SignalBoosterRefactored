using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SignalBoosterRefactored.Interfaces;
using SignalBoosterRefactored.Services;

namespace SignalBoosterRefactored
{
    /// <summary>
    /// DME data extraction and processing application.
    /// Reads physician notes, extracts relevant information, and submits to API
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Configure services and dependency injection
            var serviceProvider = ConfigureServices();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                logger.LogInformation("Starting DME data extraction process");
                
                var processor = serviceProvider.GetRequiredService<IDmeDataProcessor>();
                await processor.ProcessPhysicianNoteAsync();
                
                logger.LogInformation("DME data extraction completed successfully");
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error occurred during DME processing");
                return 1;
            }
            finally
            {
                serviceProvider?.Dispose();
            }
        }

        private static ServiceProvider ConfigureServices()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
             
            // Configure logging first
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Register configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Register HttpClient
            services.AddHttpClient();

            // Register application services with explicit constructors
            services.AddTransient<IDmeDataProcessor, DmeDataProcessor>();
            services.AddTransient<IPhysicianNoteReader, PhysicianNoteReader>();
            services.AddTransient<IDmeDataExtractor, DmeDataExtractor>();
            services.AddTransient<IApiClient, DmeApiClient>();

            return services.BuildServiceProvider();
        }
    }
}