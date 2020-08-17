using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using FundaListApp.Entities;
using System.Linq;
using FundaListApp.Services;
using System.Net.Http;

namespace FundaListApp
{
    public class App
    {
        private readonly IConfigurationRoot _config;
        private readonly ILogger<App> _logger;
        private readonly IFundaAPIClient _client;

        public App(IConfigurationRoot config, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<App>();
            _config = config;

            _client = new FundaAPIClient(new HttpClient(), _logger, _config);
        }

        public async Task Run()
        {

            Console.WriteLine("Start retrieving information from Funda.");
            _logger.LogInformation("Start retrieving information from Funda.");

            var fundaObjects = await FundaAPI.GetObjects("koop", "/amsterdam/", _client);

            DisplayMakelaars("Top 10 makelaars with 'koop' objects in Amsterdam", 
                GetTopMakelaarsByPropetiesListed(10, fundaObjects), 
                fundaObjects.Count);

            var fundaObjectsWithTuin = await FundaAPI.GetObjects("koop", "/amsterdam/tuin/", _client);

            DisplayMakelaars("Top 10 makelaars with 'koop' objects with tuin in Amsterdam",
                GetTopMakelaarsByPropetiesListed(10, fundaObjectsWithTuin),
                fundaObjectsWithTuin.Count);

            _logger.LogInformation("Done retrieving and displaying information from Funda.");

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }


        private static void DisplayMakelaars(string header, IEnumerable<Makelaar> makelaars, int totalObjects)
        {
            string format = "{0, 3} {1,-40} {2, 10}";

            Console.WriteLine();
            Console.WriteLine(header);
            Console.WriteLine();

            Console.WriteLine(format, "#", "Makelaar", "Properties");

            int i = 1;
            foreach (var makelaar in makelaars)
            {
                Console.WriteLine(format, i++, makelaar.MakelaarNaam, makelaar.PropertiesListed);
            }

            Console.WriteLine();
            Console.WriteLine($"Total number of objects in selection: {totalObjects}.");
            Console.WriteLine();
        }

        private static List<Makelaar> GetTopMakelaarsByPropetiesListed(int topN, IEnumerable<FundaObject> fundaObjects)
        {
            return fundaObjects
                .GroupBy(
                    property => property.MakelaarId,
                    (key, group) => new {
                        id = key,
                        naam = group.First().MakelaarNaam,
                        countProperties = group.Count()
                    })
                .Select(group => new Makelaar(group.naam, group.countProperties))
                .OrderByDescending(it => it.PropertiesListed)
                .Take(topN)
                .ToList();
        }
    }
}
