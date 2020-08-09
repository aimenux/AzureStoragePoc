using System.Threading.Tasks;

namespace Contracts.Ports.BlobStorage
{
    public interface IBlobClient
    {
        Task<TBlob> GetBlobAsync<TBlob, TBlobDocument>(string name)
            where TBlob : class, IBlobModel<TBlobDocument>
            where TBlobDocument : class;

        Task SaveBlobAsync<TBlob, TBlobDocument>(TBlob blob)
            where TBlob : class, IBlobModel<TBlobDocument>
            where TBlobDocument : class;
    }
}
