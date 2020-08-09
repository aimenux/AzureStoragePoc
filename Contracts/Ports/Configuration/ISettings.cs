namespace Contracts.Ports.Configuration
{
    public interface ISettings
    {
        IBlobSettings BlobSettings { get; }
        ISearchSettings SearchSettings { get; }
        ICosmosSettings CosmosSettings { get; }
    }
}
