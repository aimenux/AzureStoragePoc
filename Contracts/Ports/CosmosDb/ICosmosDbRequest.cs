namespace Contracts.Ports.CosmosDb
{
    public interface ICosmosDbRequest<TDocument> : ICosmosDbRequest where TDocument : ICosmosDbDocument
    {
        new TDocument Document { get; }
    }

    public interface ICosmosDbRequest
    {
        string Query { get; }
        object Document { get; }
    }
}
