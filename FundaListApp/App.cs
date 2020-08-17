using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using FundaListApp.Entities;
using System.Linq;

namespace FundaListApp
{
    public class App
    {
        private readonly IConfigurationRoot _config;
        private readonly ILogger<App> _logger;

        public App(IConfigurationRoot config, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<App>();
            _config = config;
        }

        public async Task Run()
        {
            List<string> emailAddresses = _config.GetSection("EmailAddresses").Get<List<string>>();
            foreach (string emailAddress in emailAddresses)
            {
                _logger.LogInformation("Email address: {@EmailAddress}", emailAddress);
            }

            Console.WriteLine("Top 10 makelaars with 'koop' objects in Amsterdam");

            var fundaObjects = FundaAPI.GetObjects("koop", "/amsterdam/");

            DisplayMakelaars(GetTopMakelaarsByPropetiesListed(10, fundaObjects));

            Console.WriteLine("Top 10 makelaars with 'koop' objects with tuin in Amsterdam");

            var fundaObjectsWithTuin = FundaAPI.GetObjects("koop", "/amsterdam/tuin/");

            DisplayMakelaars(GetTopMakelaarsByPropetiesListed(10, fundaObjectsWithTuin));

            Console.ReadKey();


        }


        private static void DisplayMakelaars(IEnumerable<Makelaar> makelaars)
        {
            string format = "{0, 3} {1,-40} {2, 10}";
            Console.WriteLine(format, "#", "Makelaar", "Number of properties");

            int i = 0;
            foreach (var makelaar in makelaars)
            {
                Console.WriteLine(format, i++, makelaar.MakelaarNaam, makelaar.PropertiesListed);
            }
        }

        private static List<Makelaar> GetTopMakelaarsByPropetiesListed(int count, IEnumerable<FundaObject> fundaObjects)
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
                .Take(count)
                .ToList();
        }
    }
}
