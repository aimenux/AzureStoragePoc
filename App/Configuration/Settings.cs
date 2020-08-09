using Contracts.Ports.Configuration;

namespace App.Configuration
{
    public class Settings : ISettings
    {
        public Settings()
        {
            BlobSettings = new BlobSettings();
            SearchSettings = new SearchSettings();
            CosmosSettings = new CosmosSettings();
        }

        public IBlobSettings BlobSettings { get; }
        public ISearchSettings SearchSettings { get; set; }
        public ICosmosSettings CosmosSettings { get; set; }
    }
}
