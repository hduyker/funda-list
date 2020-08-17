using FundaListApp.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FundaListApp.Services
{
    public class FundaAPIClient : IFundaAPIClient
    {
        private HttpClient _client;
        private ILogger<App> _logger;
        private LeakyBucket _leakyBucket;

        public FundaAPIClient(HttpClient client, ILogger<App> logger, IConfiguration config)
        {
            _client = client;
            _client.BaseAddress = new Uri($"{config["FundaAPIBaseURL"]}{config["FundaAPIKey"]}/"); 
            _logger = logger;

            // Implemented rate limiting at this point. The "Leaky Bucket" allows a maximum of 10 calls
            // per 6 seconds (which is a rate of 100 calls per minute, which is the amount indicated in
            // the documentation.
            _leakyBucket = new LeakyBucket(new BucketConfiguration
            {
                LeakRate = 10,
                LeakRateTimeSpan = TimeSpan.FromSeconds(6),
                MaxFill = 10,
                LeakResolution = 500
            });
        }

        public async Task<FundaObjectCollection> GetObjects(string type, string filter)
        {
            // Get 25 items at a time. The API won't return more than this in one page.
            const int pagesize = 25;

            var searchUriBase = $"?type={type}&zo={filter}";

            var fundaObjects = new FundaObjectCollection();

            // get pages until returned object count != pagesize, which is where you have reached the end
            int page = 1;
            int count;
            do
            {
                var pageObjects = await GetSinglePage(searchUriBase, pagesize, page++);

                // Merge the returned objects into the collection, in case there might be duplicate items 
                // returned (due to the nature of the API, which doesn't guarantee a consistent dataset when
                // iterating over pages). Note: duplicates are handled, missing items won't be detected.
                foreach (var item in pageObjects)
                {
                    if (!fundaObjects.Contains(item.Id))
                    {
                        fundaObjects.Add(item);
                    }
                }

                count = pageObjects.Count();
            } while (count == pagesize);

            return fundaObjects;
        }

        public async Task<List<FundaObject>> GetSinglePage(string searchUriBase, int pagesize, int page)
        {
            try
            {
                var pageUri = new Uri($"{searchUriBase}&page={page}&pagesize={pagesize}", UriKind.Relative);

                _logger.LogInformation($"HttpClient: Loading {pageUri}");

                // Wait for access to the Leaky Bucket queue, as a rate limiter.
                await _leakyBucket.GainAccess();

                // Using the Polly library to handle transient faults, using the Retry policy.
                // See: https://github.com/App-vNext/Polly 
                //
                // A cleaner way would be to inject the policy at the services level, but for
                // now this should work.
                //
                // According to https://cloud.google.com/solutions/rate-limiting-strategies-techniques, 
                // status code 429 = Too Many Requests, however this does not seem to be returned.
                // Instead, the service gives back a statuscode 401 (Unauthorized) with the text
                // "Request limit exceeded" in the reason.

                var res = await Policy
                    // Status codes 500, 502, 504, 503, 429
                    .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.InternalServerError)
                    .OrResult(r => r.StatusCode == HttpStatusCode.BadGateway)
                    .OrResult(r => r.StatusCode == HttpStatusCode.GatewayTimeout)
                    .OrResult(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
                    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                    // Status code 401 has additional requirements
                    .OrResult(r => r.StatusCode == HttpStatusCode.Unauthorized
                        && r.ReasonPhrase == "Request limit exceeded")
                    .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .ExecuteAsync(() => _client.GetAsync(pageUri));

                res.EnsureSuccessStatusCode();

                var resultString = await res.Content.ReadAsStringAsync();

                JObject fundaResult = JObject.Parse(resultString);

                // get JSON result objects into a list, then serialize to .NET objects
                IList<JToken> results = fundaResult["Objects"].Children().ToList();

                List<FundaObject> fundaObjects = new List<FundaObject>();

                foreach (JToken result in results)
                {
                    FundaObject fundaObject = result.ToObject<FundaObject>();
                    fundaObjects.Add(fundaObject);
                }

                return fundaObjects;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"An error occurred connecting to Funda API {ex}");
                throw;
            }
        }
    }
}
