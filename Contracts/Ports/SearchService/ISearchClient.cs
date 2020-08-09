using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contracts.Ports.SearchService
{
    public interface ISearchClient<TSearchIndex> where TSearchIndex : ISearchIndex
    {
        Task<long> SizeAsync();
        Task<long> CountAsync();
        Task DeleteIndexAndDocumentsAsync();
        Task DeleteDocumentsAsync(string keyName, ICollection<string> keysValues);
        Task SaveAsync<TSearchModel>(TSearchModel model) where TSearchModel : ISearchModel;
        Task SaveAsync<TSearchModel>(ICollection<TSearchModel> models) where TSearchModel : ISearchModel;
        Task UpdateAsync<TSearchModel>(ICollection<TSearchModel> models) where TSearchModel : ISearchModel;
        Task<ICollection<TSearchModel>> GetAsync<TSearchModel>(string query, ISearchParameters parameters = null) where TSearchModel : ISearchModel;
    }
}
