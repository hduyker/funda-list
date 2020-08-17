using FundaListApp.Entities;
using FundaListApp.Services;
using System.Linq;
using System.Threading.Tasks;

namespace FundaListApp
{
    public class FundaAPI
    {
        static public async Task<FundaObjectCollection> GetObjects(string type, string filter, IFundaAPIClient client)
        {
            // Get 25 items at a time. The API won't return more than this in one page.
            const int pagesize = 25;

            var searchUriBase = $"?type={type}&zo={filter}";

            var fundaObjects = new FundaObjectCollection();

            // get pages until returned object count != pagesize, which is where you have reached the end
            int page = 1;
            int count;
            do {
                var pageObjects = await client.GetSinglePage(searchUriBase, pagesize, page++);

                // Merge the returned objects into the collection, in case there might be duplicate items 
                // returned (due to the nature of the API, which doesn't guarantee a consistent dataset when
                // iterating over pages). Note: duplicates are handled, missing items won't be detected.
                foreach(var item in pageObjects)
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
    }
}