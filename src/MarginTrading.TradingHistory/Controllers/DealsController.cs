﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.ApiLibrary.Validation;
using MarginTrading.TradingHistory.Client;
using MarginTrading.TradingHistory.Client.Common;
using MarginTrading.TradingHistory.Client.Models;
using MarginTrading.TradingHistory.Core;
using MarginTrading.TradingHistory.Core.Domain;
using MarginTrading.TradingHistory.Core.Repositories;
using MarginTrading.TradingHistory.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.TradingHistory.Controllers
{
    [Authorize]
    [Route("api/deals")]
    public class DealsController : Controller, IDealsApi
    {
        private readonly IDealsRepository _dealsRepository;
        private readonly IConvertService _convertService;
        private readonly ILog _log;

        public DealsController(
            IDealsRepository dealsRepository,
            IConvertService convertService, 
            ILog log)
        {
            _dealsRepository = dealsRepository;
            _convertService = convertService;
            _log = log;
        }

        /// <summary>
        /// Get deals with optional filtering 
        /// </summary>
        [HttpGet, Route("")]
        public async Task<List<DealContract>> List([FromQuery] string accountId, [FromQuery] string instrument,
            [FromQuery] DateTime? closeTimeStart = null, [FromQuery] DateTime? closeTimeEnd = null)
        {
            var data = await _dealsRepository.GetAsync(accountId, instrument, closeTimeStart, closeTimeEnd);

            return data.Where(d => d != null).Select(Convert).ToList();
        }

        /// <summary>
        /// Get deals total PnL with optional filtering by period
        /// </summary>
        /// <param name="accountId">The account id</param>
        /// <param name="instrument">The instrument id</param>
        /// <param name="closeTimeStart"></param>
        /// <param name="closeTimeEnd"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("totalPnl")]
        public async Task<TotalPnlContract> GetTotalPnL([FromQuery] string accountId, [FromQuery] string instrument,
            [FromQuery] DateTime? closeTimeStart = null, [FromQuery] DateTime? closeTimeEnd = null)
        {
            var totalPnl = await _dealsRepository.GetTotalPnlAsync(accountId, instrument, closeTimeStart, closeTimeEnd);

            return new TotalPnlContract {Value = totalPnl};
        }

        /// <summary>
        /// Get total profit of deals with filtering by set of days
        /// </summary>
        /// <param name="accountId">The account id</param>
        /// <param name="days">The days array</param>
        /// <returns></returns>
        [HttpGet]
        [Route("totalProfit")]
        public async Task<TotalProfitContract> GetTotalProfit(string accountId, DateTime[] days)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                await _log.WriteWarningAsync(
                    nameof(DealsController), 
                    nameof(GetTotalProfit), 
                    null,
                    $"{nameof(accountId)} value is not valid");
                
                return TotalProfitContract.Empty();
            }

            if (days == null || days.Length == 0)
            {
                await _log.WriteWarningAsync(
                    nameof(DealsController), 
                    nameof(GetTotalProfit), 
                    null,
                    $"{nameof(days)} value is not valid");
                
                return TotalProfitContract.Empty();
            }

            var totalProfit = await _dealsRepository.GetTotalProfitAsync(accountId, days);

            return new TotalProfitContract {Value = totalProfit};
        }

        /// <summary> 
        /// Get deals with optional filtering and pagination 
        /// </summary>
        [HttpGet, Route("by-pages")]
        public async Task<PaginatedResponseContract<DealContract>> ListByPages(
            [FromQuery] string accountId, [FromQuery] string instrument, 
            [FromQuery] DateTime? closeTimeStart = null, [FromQuery] DateTime? closeTimeEnd = null,
            [FromQuery] int? skip = null, [FromQuery] int? take = null,
            [FromQuery] bool isAscending = false)
        {
            ApiValidationHelper.ValidatePagingParams(skip, take);
            
            var data = await _dealsRepository.GetByPagesAsync(accountId, instrument, 
                closeTimeStart, closeTimeEnd, skip: skip, take: take, isAscending: isAscending);

            return new PaginatedResponseContract<DealContract>(
                contents: data.Contents.Select(Convert).ToList(),
                start: data.Start,
                size: data.Size,
                totalSize: data.TotalSize
            );
        }

        /// <summary> 
        /// Get deals with optional filtering and pagination 
        /// </summary>
        [HttpGet, Route("aggregated")]
        [ValidateModel]
        public async Task<PaginatedResponseContract<AggregatedDealContract>> GetAggregated(
            [FromQuery] [Required] string accountId, [FromQuery] string instrument,
            [FromQuery] DateTime? closeTimeStart = null, [FromQuery] DateTime? closeTimeEnd = null,
            [FromQuery] int? skip = null, [FromQuery] int? take = null,
            [FromQuery] bool isAscending = false)
        {
            ApiValidationHelper.ValidateAggregatedParams(accountId, skip, take);

            var data = await _dealsRepository.GetAggregated(accountId, instrument,
                closeTimeStart, closeTimeEnd, skip: skip, take: take, isAscending: isAscending);

            return new PaginatedResponseContract<AggregatedDealContract>(
                contents: data.Contents.Select(Convert).ToList(),
                start: data.Start,
                size: data.Size,
                totalSize: data.TotalSize
            );
        }

        /// <summary>
        /// Get deal by Id
        /// </summary>
        /// <param name="dealId"></param>
        /// <returns></returns>
        [HttpGet, Route("{dealId}")]
        public async Task<DealContract> ById(string dealId)
        {
            if (string.IsNullOrWhiteSpace(dealId))
            {
                throw new ArgumentException("Deal id must be set", nameof(dealId));
            }
            
            var deal = await _dealsRepository.GetAsync(dealId);

            return deal == null ? null : Convert(deal);
        }

        private DealContract Convert(IDeal deal)
        {
            return _convertService.Convert<IDeal, DealContract>(deal, opts => opts.ConfigureMap()
                .ForMember(x => x.Direction, 
                    o => o.ResolveUsing(z => z.Direction.ToType<PositionDirectionContract>())));
        }

        private AggregatedDealContract Convert(IAggregatedDeal aggregate)
        {
            return _convertService.Convert<IAggregatedDeal, AggregatedDealContract>(aggregate, opts => opts.ConfigureMap());
        }
    }
}
