using Contracts.Ports.CosmosDb.Documents;

namespace Contracts.Models
{
    public class Data : IData
    {
        public Request Request { get; set; }
        public Response Response { get; set; }
    }
}
