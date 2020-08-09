using Contracts.Ports.CosmosDb.Documents;

namespace Contracts.Payloads
{
    public class Payload : IPayload
    {
        public RequestDto RequestDto { get; set; }
        public ResponseDto ResponseDto { get; set; }
    }
}
