using Contracts.Ports.Configuration;

namespace App.Configuration
{
    public class BlobSettings : IBlobSettings
    {
        public string ConnectionString { get; set; }
    }
}