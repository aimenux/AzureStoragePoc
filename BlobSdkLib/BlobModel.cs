using System.Collections.Generic;
using Contracts.Ports.BlobStorage;

namespace BlobSdkLib
{
    public class BlobModel<TBlobDocument> : IBlobModel<TBlobDocument>
    {
        public string Name { get; set; }
        public TBlobDocument Document { get; set; }
        public IDictionary<string, string> Metadata { get; set; }
    }
}
