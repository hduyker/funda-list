using System;
using System.Collections.Generic;
using System.Text;

namespace FundaListApp.Entities
{
    class SearchResult
    {
        public List<FundaObject> FundaObjects { get; set; }
        public PagingInfo Paging { get; set; }
        public int TotaalAantalObjecten { get; set; }

    }
}
