namespace Contracts.Ports.Configuration
{
    public interface ISearchSettings
    {
        string Name { get; }
        string ApiKey { get; }
        string IndexName { get; }
    }
}