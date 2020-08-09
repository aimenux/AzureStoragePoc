using System;
using System.Linq;
using Contracts.Builders;
using Contracts.Ports.SearchService;
using SearchDocumentsJob.Models;

namespace SearchDocumentsJob.Builders
{
    public class TransactionIdSearchModelBuilder : ISearchModelBuilder
    {
        private readonly int _nbrOrders;

        public TransactionIdSearchModelBuilder(int nbrOrders)
        {
            _nbrOrders = nbrOrders;
        }

        public ISearchModel BuildSearchModel()
        {
            var orderIds = Enumerable
                .Range(0, _nbrOrders)
                .Select(x => Guid.NewGuid().ToString())
                .ToList();

            return new TransactionIdSearchModel
            {
                OrderIds = orderIds,
                TransactionId = Guid.NewGuid().ToString(),
                TransactionDate = RandomBuilderHelper.RandomDate()
            };
        }
    }
}