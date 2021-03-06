﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FundaListApp
{
    class Program
    {
        public static IConfigurationRoot configuration;

        static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File($"FundaList-log-{DateTime.Now:yyyy-MM-dd}.txt")
                .CreateLogger();

            try
            {
                MainAsync(args).Wait();
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        static async Task MainAsync(string[] args)
        {
            // Create service collection
            Log.Information("Creating service collection");
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // Create service provider
            Log.Information("Building service provider");
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                Log.Information("Starting application");
                await serviceProvider.GetService<App>().Run();
                Log.Information("Ending application");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error running application");
                throw ex;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                // Add logging
                .AddSingleton(LoggerFactory.Create(builder => {
                    builder.AddSerilog(dispose: true);
                }))
                .AddLogging();

            const string configurationFile = "appsettings.json";
            var basePath = Directory.GetParent(AppContext.BaseDirectory).FullName;
            // Build configuration
            try
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile(configurationFile, false)
                    .Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot access the configuration file {configurationFile} at {basePath}.");
                Log.Fatal($"Cannot access configuration file {configurationFile} at {basePath}: {ex}");
                throw;
            }

            // Assumption is that the API key is always 32 characters.
            if (configuration["FundaAPIKey"].Length != 32)
            {
                Console.WriteLine($"API key seems wrong in the configuration file {configurationFile} in {basePath}.");
                throw new InvalidOperationException("API key seems incorrect.");
            }

            // Add access to generic IConfigurationRoot, add FundaAPI HTTP Client and App
            serviceCollection
                .AddSingleton<IConfigurationRoot>(configuration)
                .AddTransient<App>();
        }
    }
}
