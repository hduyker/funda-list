using FundaListApp.Entities;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FundaListApp
{
    class FundaAPI
    {
        public static FundaObjectCollection GetObjects(string type, string filter)
        {
            // Get 25 items at a time. The API won't return more than this in one page.
            const int pagesize = 25;

            var searchUriBase = $"/feeds/Aanbod.svc/[key]/?type={type}&zo={filter}";

            var fundaObjects = new FundaObjectCollection();

            // get pages until returned object count != pagesize, which is where you have reached the end
            int page = 1;
            int count;
            {
                var pageObjects = GetSinglePage(searchUriBase, pagesize, page++);

                // Merge the returned objects into the collection, in case there might be duplicate items 
                // returned (due to the nature of the API). Note: missing items won't be detected.
                foreach(var item in pageObjects)
                {
                    if (!fundaObjects.Contains(item.Id))
                    {
                        fundaObjects.Add(item);
                    }
                }

                count = pageObjects.Count;
            } while (count == pagesize);

            return fundaObjects;
        }

        private static List<FundaObject> GetSinglePage(string searchUriBase, int pagesize, int page)
        {
            var searchUri = $"{searchUriBase}&page={page}&pagesize={pagesize}";

            var resultString = "";

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
    }
}