using System;
namespace FundaListApp.Entities
{
    public class Makelaar
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
