namespace Contracts.Ports.Configuration
{
    public interface ICosmosSettings
    {
        string Url { get; }
        string Key { get; }
        string DatabaseName { get; }
    }
}