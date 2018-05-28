﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.TradingHistory.Client;
using MarginTrading.TradingHistory.Client.Models;
using MarginTrading.TradingHistory.Core;
using MarginTrading.TradingHistory.Core.Domain;
using MarginTrading.TradingHistory.Core.Repositories;
using MarginTrading.TradingHistory.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.TradingHistory.Controllers
{
    [Route("api/positions-history")]
    public class PositionsController : Controller, IPositionsHistoryApi
    {
        private readonly IOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IConvertService _convertService;
        
        public PositionsController(
            IOrdersHistoryRepository ordersHistoryRepository,
            IConvertService convertService)
        {
            _ordersHistoryRepository = ordersHistoryRepository;
            _convertService = convertService;
        }
        
        /// <summary> 
        /// Get closed positions with optional filtering 
        /// </summary> 
        [HttpGet("")] 
        public async Task<List<PositionContract>> PositionHistory(
            [FromQuery] string accountId, [FromQuery] string instrument)
        {
            var orders = await _ordersHistoryRepository.GetHistoryAsync(x =>
                x.OrderUpdateType == OrderUpdateType.Close &&
                (string.IsNullOrEmpty(accountId) || x.AccountId == accountId)
                && (string.IsNullOrEmpty(instrument) || x.Instrument == instrument));
            
            return orders.Select(Convert).ToList();
        }

        private PositionContract Convert(IOrderHistory orderHistory)
        {
            return new PositionContract
            {
                Id = orderHistory.Id,
                AccountId = orderHistory.AccountId,
                Instrument = orderHistory.Instrument,
                Timestamp = orderHistory.OpenDate ?? orderHistory.CreateDate,
                Direction = ConvertDirection(orderHistory.Type),
                Price = orderHistory.ClosePrice == default ? orderHistory.OpenPrice : orderHistory.ClosePrice,
                Volume = -orderHistory.Volume,
                PnL = orderHistory.PnL,
                TradeId = orderHistory.Id, //TODO need to be fixed
                RelatedOrders = new List<string>(),//TODO need to be fixed
            };
        }

        private PositionDirection ConvertDirection(OrderDirection type)
        {
            switch (type)
            {
                case OrderDirection.Buy: return PositionDirection.Long;
                case OrderDirection.Sell: return PositionDirection.Short;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
