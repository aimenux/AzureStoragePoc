using System.Threading.Tasks;

namespace Contracts.Ports.CosmosDb
{
    public interface ICosmosDbClient<TDocument> where TDocument : ICosmosDbDocument
    {
        Task<int> ReadThroughputAsync();
        Task<ICosmosDbResponse<TDocument>> InsertAsync(ICosmosDbRequest<TDocument> request);
        Task<ICosmosDbResponse<TDocument>> GetAsync(ICosmosDbRequest<TDocument> request);
    }

    public interface ICosmosDbClient
    {
        Task<int> ReadThroughputAsync();
        Task<ICosmosDbResponse> InsertAsync(ICosmosDbRequest request);
        Task<ICosmosDbResponse> GetAsync(ICosmosDbRequest request);
    }
}
