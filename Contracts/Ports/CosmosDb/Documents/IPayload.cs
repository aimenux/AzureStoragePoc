using Contracts.Payloads;

namespace Contracts.Ports.CosmosDb.Documents
{
    public interface IPayload
    {
        RequestDto RequestDto { get; }
        ResponseDto ResponseDto { get; }
    }
}
