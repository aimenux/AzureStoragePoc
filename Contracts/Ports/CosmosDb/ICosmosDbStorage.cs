using System.Threading.Tasks;
using Contracts.Ports.CosmosDb.Documents;

namespace Contracts.Ports.CosmosDb
{
    public interface ICosmosDbStorage
    {
        Task<ICosmosDbResponse> GetAsync(string orderId);
        Task<ICosmosDbResponse> SaveAsync(IPayload payload, IData data);
    }
}
