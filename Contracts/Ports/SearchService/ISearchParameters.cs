namespace Contracts.Ports.SearchService
{
    public interface ISearchParameters
    {
        int? Top { get; }
        string Filter { get; }
    }
}