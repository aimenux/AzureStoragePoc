using Contracts.Ports.CosmosDb.Documents;

namespace Contracts.Builders
{
    public interface IContractsBuilder
    {
        IData BuildDataContracts(string transactionId);
        IPayload BuildPayloadContracts(string transactionId);
    }
}
