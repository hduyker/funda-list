using System;
using System.Collections.Generic;
using System.Text;

namespace FundaListApp.Entities
{
    class Makelaar
    {
        public Makelaar(string makelaarNaam, int propertiesListed)
        {
            MakelaarNaam = makelaarNaam ?? throw new ArgumentNullException(nameof(makelaarNaam));
            PropertiesListed = propertiesListed;
        }

        public string MakelaarNaam { get; set; }
        public int PropertiesListed { get; set; }
    }
}
