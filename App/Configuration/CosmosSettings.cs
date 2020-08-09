using Contracts.Ports.Configuration;

namespace App.Configuration
{
    public class CosmosSettings : ICosmosSettings
    {
        public string Url { get; set; }
        public string Key { get; set; }
        public string DatabaseName { get; set; }
    }
}