using Contracts.Ports.Configuration;

namespace App.Configuration
{
    public class SearchSettings : ISearchSettings
    {
        public string Name { get; set; }
        public string ApiKey { get; set; }
        public string IndexName { get; set; }
    }
}