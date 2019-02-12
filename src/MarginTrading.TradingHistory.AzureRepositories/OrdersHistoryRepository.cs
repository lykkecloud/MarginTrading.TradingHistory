﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.TradingHistory.AzureRepositories.Entities;
using MarginTrading.TradingHistory.Core;
using MarginTrading.TradingHistory.Core.Domain;
using MarginTrading.TradingHistory.Core.Repositories;

namespace MarginTrading.TradingHistory.AzureRepositories
{
    public class OrdersHistoryRepository : IOrdersHistoryRepository
    {
        private readonly INoSQLTableStorage<OrderHistoryEntity> _tableStorage;

        public OrdersHistoryRepository(INoSQLTableStorage<OrderHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddAsync(IOrderHistory order)
        {
            var entity = OrderHistoryEntity.Create(order);
            // ReSharper disable once RedundantArgumentDefaultValue
            //TODO: use event datetime
            return _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity,
                DateTime.UtcNow, RowKeyDateTimeFormat.Iso);
        }

        public async Task<IEnumerable<IOrderHistory>> GetHistoryAsync(string accountId, string assetPairId,
            OrderStatus? status = null, bool withRelated = false,
            DateTime? createdTimeStart = null, DateTime? createdTimeEnd = null,
            DateTime? modifiedTimeStart = null, DateTime? modifiedTimeEnd = null)
        {
            var entities = await _tableStorage.GetDataAsync(x =>
                (string.IsNullOrWhiteSpace(accountId) || x.AccountId == accountId)
                && (string.IsNullOrWhiteSpace(assetPairId) || x.AssetPairId == assetPairId)
                && (status == null || x.Status == status)
                && (createdTimeStart == null || x.CreatedTimestamp >= createdTimeStart)
                && (createdTimeEnd == null || x.CreatedTimestamp < createdTimeEnd)
                && (modifiedTimeStart == null || x.ModifiedTimestamp >= modifiedTimeStart)
                && (modifiedTimeEnd == null || x.ModifiedTimestamp < modifiedTimeEnd));

            var related = withRelated
                ? await _tableStorage.GetDataAsync(x => entities.Select(e => e.Id).Contains(x.ParentOrderId))
                : new List<OrderHistoryEntity>();

            return entities.Concat(related).OrderByDescending(entity => entity.ModifiedTimestamp);
        }

        public async Task<IEnumerable<IOrderHistory>> GetHistoryAsync(string orderId, 
            OrderStatus? status = null, bool withRelated = false)
        {
            var entities = await _tableStorage.GetDataAsync(x => x.Id == orderId
                                                                 || (withRelated && x.ParentOrderId == orderId)
                                                                 || (status == null || x.Status == status));

            return entities;
        }

        public async Task<PaginatedResponse<IOrderHistory>> GetHistoryByPagesAsync(string accountId, string assetPairId,
            List<OrderStatus> statuses, List<OrderType> orderTypes, List<OriginatorType> originatorTypes,
            bool withRelated, string parentOrderId = null,
            DateTime? createdTimeStart = null, DateTime? createdTimeEnd = null,
            DateTime? modifiedTimeStart = null, DateTime? modifiedTimeEnd = null,
            int? skip = null, int? take = null, bool isAscending = true)
        {
            var entities = await _tableStorage.GetDataAsync(x =>
                (string.IsNullOrWhiteSpace(accountId) || x.AccountId == accountId)
                && (string.IsNullOrWhiteSpace(assetPairId) || x.AssetPairId == assetPairId)
                && (statuses == null || statuses.Count == 0 || statuses.Any(s => s == x.Status))
                && (orderTypes == null || orderTypes.Count == 0 || orderTypes.Any(s => s == x.Type))
                && (originatorTypes == null || originatorTypes.Count == 0 || originatorTypes.Any(s => s == x.Originator))
                && (createdTimeStart == null || x.CreatedTimestamp >= createdTimeStart)
                && (createdTimeEnd == null || x.CreatedTimestamp < createdTimeEnd)
                && (modifiedTimeStart == null || x.ModifiedTimestamp >= modifiedTimeStart)
                && (modifiedTimeEnd == null || x.ModifiedTimestamp < modifiedTimeEnd));

            var related = withRelated || !string.IsNullOrEmpty(parentOrderId)
                ? await _tableStorage.GetDataAsync(x => entities.Select(e => e.Id).Contains(x.ParentOrderId))
                : new List<OrderHistoryEntity>();

            var allData = entities.Concat(related).OrderByDescending(entity => entity.ModifiedTimestamp).ToList();

            allData = string.IsNullOrEmpty(parentOrderId) ? allData : allData.Where(x => x.ParentOrderId == parentOrderId).ToList();

            //TODO refactor before using azure impl
            var data = (isAscending
                    ? allData.OrderBy(x => x.CreatedTimestamp)
                    : allData.OrderByDescending(x => x.CreatedTimestamp))
                .ToList();
            var filtered = take.HasValue ? data.Skip(skip.Value).Take(take.Value).ToList() : data;
            
            return new PaginatedResponse<IOrderHistory>(
                contents: filtered,
                start: skip ?? 0,
                size: filtered.Count,
                totalSize: data.Count
            );
        }
    }
}
