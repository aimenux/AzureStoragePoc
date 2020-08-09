using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.Ports.CosmosDb;

namespace CosmosSdkLib
{
    public class CosmosDbResponse<TDocument> : ICosmosDbResponse<TDocument> where TDocument : ICosmosDbDocument
    {
        protected ICollection<object> DynamicDocuments;

        public CosmosDbResponse(double requestUnits, params TDocument[] documents)
        {
            RequestUnits = Math.Round(requestUnits, 3);
            Documents = documents ?? Array.Empty<TDocument>();
            DynamicDocuments = Documents.Cast<object>().ToList();
            DynamicInformations = new Dictionary<string, object>();
        }

        public double RequestUnits { get; }

        public ICollection<TDocument> Documents { get; }

        ICollection<object> ICosmosDbResponse.Documents => DynamicDocuments;

        public IDictionary<string, object> DynamicInformations { get; set; }
    }

    public class CosmosDbResponse : CosmosDbResponse<ICosmosDbDocument>
    {
        public CosmosDbResponse(double requestUnits, params ICosmosDbDocument[] documents) : base(requestUnits, documents)
        {
        }

        public CosmosDbResponse(double requestUnits, IEnumerable<object> documents) : this(requestUnits)
        {
            DynamicDocuments = documents.ToList();
        }
    }
}
