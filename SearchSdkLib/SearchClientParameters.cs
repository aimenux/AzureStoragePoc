using Contracts.Ports.SearchService;
using Microsoft.Azure.Search.Models;

namespace SearchSdkLib
{
    public class SearchClientParameters : SearchParameters, ISearchParameters
    {
        public SearchClientParameters()
        {
        }

        public SearchClientParameters(ISearchParameters parameters)
        {
            Top = parameters?.Top;
            Filter = parameters?.Filter;
        }
    }
}
