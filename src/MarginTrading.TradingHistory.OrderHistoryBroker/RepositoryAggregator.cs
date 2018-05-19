﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.TradingHistory.Core.Domain;
using MarginTrading.TradingHistory.Core.Repositories;

namespace MarginTrading.TradingHistory.OrderHistoryBroker
{
    internal class RepositoryAggregator : IOrdersHistoryRepository
    {
        private readonly List<IOrdersHistoryRepository> _repositories;

        public RepositoryAggregator(IEnumerable<IOrdersHistoryRepository> repositories)
        {
            _repositories = new List<IOrdersHistoryRepository>();
            _repositories.AddRange(repositories);
        }

        public async Task AddAsync(OrderHistory report)
        {
            foreach (var item in _repositories)
            {
                await item.AddAsync(report);
            }
        }

        public async Task<IEnumerable<OrderHistory>> GetHistoryAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyList<OrderHistory>> GetHistoryAsync(string[] accountIds, DateTime? @from, DateTime? to)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<OrderHistory>> GetHistoryAsync(Func<OrderHistory, bool> predicate)
        {
            throw new NotImplementedException();
        }
    }
}
