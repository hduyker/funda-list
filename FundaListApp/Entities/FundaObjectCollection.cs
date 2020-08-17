using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FundaListApp.Entities
{
    class FundaObjectCollection : KeyedCollection<string, FundaObject>
    {
        protected override string GetKeyForItem(FundaObject item)
        {
            return item.Id;
        }
    }
}
