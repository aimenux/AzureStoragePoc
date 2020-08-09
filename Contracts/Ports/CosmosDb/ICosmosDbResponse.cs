using System.Collections.Generic;

namespace Contracts.Ports.CosmosDb
{
    public interface ICosmosDbResponse<TDocument> : ICosmosDbResponse where TDocument : ICosmosDbDocument
    {
        new ICollection<TDocument> Documents { get; }
    }

    public interface ICosmosDbResponse
    {
        double RequestUnits { get; }
        ICollection<object> Documents { get; }
        IDictionary<string, object> DynamicInformations { get; }
    }
}