using FundaListApp;
using FundaListApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using Xunit;

namespace FundaListAppTests
{
    /// <summary>
    /// Test the Funda API
    /// </summary>
    public class TestFundaApi
    {
        private static FundaAPIClient _client;

        [Fact]
        public void RetrieveSingleItem()
        {
            // Arrange
            Configure();
            var uriBase = "?type=koop&zo=/amsterdam/tuin";

            // Act
            var result = _client.GetSinglePage(uriBase, 1, 1).Result;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }

        [Fact]
        public void RetrieveSinglePage()
        {
            // Arrange
            Configure();
            var uriBase = "?type=koop&zo=/amsterdam/tuin"; 

            // Act
            var result = _client.GetSinglePage(uriBase, 25, 1).Result;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }

        private static void Configure()
        {
            ServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection
                .AddSingleton(LoggerFactory.Create(builder => {
                    builder.AddSerilog(dispose: true);
                }))
                .AddLogging();

            IConfigurationRoot configuration;

            const string configurationFile = "appsettings.json";
            var basePath = Directory.GetParent(AppContext.BaseDirectory).FullName;
            // Build configuration
            try
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile(configurationFile, false)
                    .Build();

                if (configuration["FundaAPIKey"].Length != 32)
                {
                    throw new InvalidOperationException("API key seems incorrect.");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"Cannot access configuration file {basePath}{configurationFile}: {ex}");
                throw;
            }


            // Add access to generic IConfigurationRoot, add FundaAPI HTTP Client and App
            serviceCollection
                .AddSingleton<IConfigurationRoot>(configuration)
                .AddTransient<App>();

            ILogger<FundaListApp.App> logger = new Logger<FundaListApp.App>(new LoggerFactory());

            _client = new FundaAPIClient(new HttpClient(), logger, configuration);
        }
    }
}
