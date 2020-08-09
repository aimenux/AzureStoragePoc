using System.Collections.Generic;

namespace Contracts.Ports.BlobStorage
{
    public interface IBlobModel<TBlobDocument>
    {
        string Name { get; set; }
        TBlobDocument Document { get; set; }
        IDictionary<string, string> Metadata { get; set; }
    }
}
