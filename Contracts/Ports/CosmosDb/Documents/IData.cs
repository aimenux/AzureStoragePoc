using Contracts.Models;

namespace Contracts.Ports.CosmosDb.Documents
{
    public interface IData
    {
        Request Request { get; }
        Response Response { get; }
    }
}