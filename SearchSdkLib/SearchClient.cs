using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contracts.Extensions;
using Contracts.Ports.Configuration;
using Contracts.Ports.SearchService;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Nito.AsyncEx;

namespace SearchSdkLib
{
    public sealed class SearchClient<TSearchIndex> : ISearchClient<TSearchIndex>, IDisposable where TSearchIndex : ISearchIndex
    {
        private readonly ISearchSettings _searchSettings;
        private readonly ISearchIndexClient _searchIndexClient;
        private readonly SearchServiceClient _searchServiceClient;
        private readonly TimeSpan _lockerTimeout = TimeSpan.FromMinutes(5);
        private readonly AsyncSemaphore _asyncSemaphore = new AsyncSemaphore(100);

        public SearchClient(ISearchSettings settings)
        {
            _searchSettings = settings;
            var credentials = new SearchCredentials(settings.ApiKey);
            _searchServiceClient = new SearchServiceClient(settings.Name, credentials);
            _searchIndexClient = GetOrCreateSearchIndexClient();
        }

        public async Task<long> SizeAsync()
        {
            var stats = await _searchServiceClient.Indexes.GetStatisticsAsync(_searchSettings.IndexName);
            return stats.StorageSize;
        }

        public Task<long> CountAsync()
        {
            return _searchIndexClient.Documents.CountAsync();
        }

        public Task DeleteIndexAndDocumentsAsync()
        {
            return _searchServiceClient.Indexes.DeleteAsync(_searchSettings.IndexName);
        }

        public Task DeleteDocumentsAsync(string keyName, ICollection<string> keysValues)
        {
            var batch = IndexBatch.Delete(keyName, keysValues);
            return RunBatchAsync(batch);
        }

        public Task SaveAsync<TSearchModel>(TSearchModel model) where TSearchModel : ISearchModel
        {
            var models = new[] {model};
            return SaveAsync(models);
        }

        public Task SaveAsync<TSearchModel>(ICollection<TSearchModel> models) where TSearchModel : ISearchModel
        {
            var actions = models.Select(IndexAction.Upload);
            var batch = IndexBatch.New(actions);
            return RunBatchAsync(batch);
        }

        public Task UpdateAsync<TSearchModel>(ICollection<TSearchModel> models) where TSearchModel : ISearchModel
        {
            var actions = models.Select(IndexAction.MergeOrUpload);
            var batch = IndexBatch.New(actions);
            return RunBatchAsync(batch);
        }

        public async Task<ICollection<TSearchModel>> GetAsync<TSearchModel>(string query, ISearchParameters parameters = null) where TSearchModel : ISearchModel
        {
            var searchParameters = new SearchClientParameters(parameters);
            var searchResults = await _searchIndexClient.Documents.SearchAsync<TSearchModel>(query, searchParameters);
            var foundResults = searchResults.Results.Select(x => x.Document).ToList();
            return foundResults;
        }

        public void Dispose()
        {
            _searchIndexClient?.Dispose();
            _searchServiceClient?.Dispose();
        }

        private ISearchIndexClient GetOrCreateSearchIndexClient()
        {
            if (_searchServiceClient.Indexes.Exists(_searchSettings.IndexName))
            {
                return _searchServiceClient.Indexes.GetClient(_searchSettings.IndexName);
            }

            var indexDefinition = new Index
            {
                Name = _searchSettings.IndexName,
                Fields = FieldBuilder.BuildForType<TSearchIndex>()
            };

            var index = _searchServiceClient.Indexes.Create(indexDefinition);
            return _searchServiceClient.Indexes.GetClient(index.Name);
        }

        private async Task RunBatchAsync<T>(IndexBatch<T> batch)
        {
            try
            {
                var cancellationToken = new CancellationTokenSource(_lockerTimeout).Token;
                await _asyncSemaphore.WaitAsync(cancellationToken);
                await _searchIndexClient.Documents.IndexAsync(batch, cancellationToken: cancellationToken);
            }
            catch (IndexBatchException ex)
            {
                var keys = ex.IndexingResults
                    .Where(r => !r.Succeeded)
                    .Select(r => r.Key);
                var failedDocuments = $"Keys ({string.Join(", ", keys)})";
                ConsoleColor.Red.WriteLine($"Failed to index some documents: {failedDocuments} [{ex.Message}]");
            }
            catch (Exception ex)
            {
                ConsoleColor.Red.WriteLine($"Transient exception occured: '{ex.GetType().Name}' [{ex.Message}] ({_asyncSemaphore.CurrentCount})");
            }
            finally
            {
                _asyncSemaphore.Release();
            }
        }
    }
}
