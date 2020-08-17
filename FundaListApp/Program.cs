using FundaListApp.Entities;
using FundaListApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;

namespace FundaListApp
{
    class Program
    {
        public static IConfigurationRoot configuration;

        static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("FundaList-log-{Date}.txt")
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

            // Print connection string to demonstrate configuration object is populated
            Console.WriteLine(configuration.GetConnectionString("DataConnection"));

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

            // Build configuration
             configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Add access to generic IConfigurationRoot, add FundaAPI HTTP Client and App
            serviceCollection
                .AddSingleton<IConfigurationRoot>(configuration)
                .AddTransient<App>();
        }
    }
}
