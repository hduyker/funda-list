using FundaListApp.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FundaListApp.Services
{
    public interface IFundaAPIClient
    {
        Task<FundaObjectCollection> GetObjects(string type, string filter);
        Task<List<FundaObject>> GetSinglePage(string searchUriBase, int pagesize, int page);
    }
}