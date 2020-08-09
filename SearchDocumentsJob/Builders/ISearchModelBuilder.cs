using Contracts.Ports.SearchService;

namespace SearchDocumentsJob.Builders
{
    public interface ISearchModelBuilder
    {
        ISearchModel BuildSearchModel();
    }
}
