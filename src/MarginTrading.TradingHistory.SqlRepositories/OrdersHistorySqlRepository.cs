﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using MarginTrading.TradingHistory.Core.Domain;
using MarginTrading.TradingHistory.Core.Repositories;

namespace MarginTrading.TradingHistory.SqlRepositories
{
    public class OrdersHistorySqlRepository : IOrdersHistoryRepository
    {
        private const string TableName = "OrdersChangeHistory";

        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 @"[OID] [int] NOT NULL IDENTITY (1,1) PRIMARY KEY,
[Id] [nvarchar](64) NOT NULL,
[Code] [bigint] NULL,
[ClientId] [nvarchar] (64) NOT NULL,
[TradingConditionId] [nvarchar] (64) NOT NULL,
[AccountAssetId] [nvarchar] (64) NULL,
[Instrument] [nvarchar] (64) NOT NULL,
[Type] [nvarchar] (64) NOT NULL,
[CreateDate] [datetime] NOT NULL,
[OpenDate] [datetime] NULL,
[CloseDate] [datetime] NULL,
[ExpectedOpenPrice] [float] NULL,
[OpenPrice] [float] NULL,
[ClosePrice] [float] NULL,
[QuoteRate] [float] NULL,
[Volume] [float] NULL,
[TakeProfit] [float] NULL,
[StopLoss] [float] NULL,
[CommissionLot] [float] NULL,
[OpenCommission] [float] NULL,
[CloseCommission] [float] NULL,
[SwapCommission] [float] NULL,
[EquivalentAsset] [nvarchar] (64) NULL,
[OpenPriceEquivalent] [float] NULL,
[ClosePriceEquivalent] [float] NULL,
[StartClosingDate] [datetime] NULL,
[Status] [nvarchar] (64) NULL,
[CloseReason] [nvarchar] (64) NULL,
[FillType] [nvarchar] (64) NULL,
[RejectReason] [nvarchar] (64) NULL,
[RejectReasonText] [nvarchar] (255) NULL,
[Comment] [nvarchar] (255) NULL,
[MatchedVolume] [float] NULL,
[MatchedCloseVolume] [float] NULL,
[Fpl] [float] NULL,
[PnL] [float] NULL,
[InterestRateSwap] [float] NULL,
[MarginInit] [float] NULL,
[MarginMaintenance] [float] NULL,
[OrderUpdateType] [nvarchar] (64) NULL,
[OpenExternalOrderId] [nvarchar] (64) NULL,
[OpenExternalProviderId] [nvarchar] (64) NULL,
[CloseExternalOrderId] [nvarchar] (64) NULL,
[CloseExternalProviderId] [nvarchar] (64) NULL,
[MatchingEngineMode] [nvarchar] (64) NULL,
[LegalEntity] [nvarchar] (64) NULL);";

        private readonly string _reportsSqlConnString;
        private readonly ILog _log;
        private IOrdersHistoryRepository _ordersHistoryRepositoryImplementation;

        public OrdersHistorySqlRepository(string reportsSqlConnString, ILog log)
        {
            _reportsSqlConnString = reportsSqlConnString;
            _log = log;
            
            using (var conn = new SqlConnection(reportsSqlConnString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync("OrdersChangeHistory", "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task AddAsync(IOrderHistory order)
        {
            using (var conn = new SqlConnection(_reportsSqlConnString))
            {
                var query = $"insert into {TableName} " +
                            @"(Id, Code, ClientId, TradingConditionId, AccountAssetId, Instrument, Type, CreateDate, OpenDate,
                            CloseDate, ExpectedOpenPrice, OpenPrice, ClosePrice, QuoteRate, Volume, TakeProfit, 
                            StopLoss, CommissionLot, OpenCommission, CloseCommission, SwapCommission, StartClosingDate, 
                            Status, CloseReason, FillType, RejectReason, RejectReasonText, Comment, MatchedVolume,
                            MatchedCloseVolume, Fpl, PnL, InterestRateSwap, MarginInit, MarginMaintenance, 
                            OrderUpdateType, OpenExternalOrderId, OpenExternalProviderId, CloseExternalOrderId,
                            CloseExternalProviderId, MatchingEngineMode, LegalEntity) 
                             values 
                            (@Id, @Code, @ClientId, @TradingConditionId, @AccountAssetId, @Instrument, @Type, @CreateDate, @OpenDate,
                            @CloseDate, @ExpectedOpenPrice, @OpenPrice, @ClosePrice, @QuoteRate, @Volume, @TakeProfit, 
                            @StopLoss, @CommissionLot, @OpenCommission, @CloseCommission, @SwapCommission, @StartClosingDate, 
                            @Status, @CloseReason, @FillType, @RejectReason, @RejectReasonText, @Comment, @MatchedVolume,
                            @MatchedCloseVolume, @Fpl, @PnL, @InterestRateSwap, @MarginInit, @MarginMaintenance, 
                            @OrderUpdateType, @OpenExternalOrderId, @OpenExternalProviderId, @CloseExternalOrderId,
                            @CloseExternalProviderId, @MatchingEngineMode, @LegalEntity)";

                try
                {
                    var entity = OrderHistoryEntity.Create(order);
                    await conn.ExecuteAsync(query, entity);
                }
                catch (Exception ex)
                {
                    var msg = $"Error {ex.Message} \n" +
                              "Entity <IOrderHistory>: \n" +
                              order.ToJson();
                    
                    _log?.WriteWarning("AccountTransactionsReportsSqlRepository", "InsertOrReplaceAsync", msg);
                    
                    throw new Exception(msg);
                }
            }
        }

        public async Task<IEnumerable<IOrderHistory>> GetHistoryAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyList<IOrderHistory>> GetHistoryAsync(string[] accountIds, DateTime? @from, DateTime? to)
        {
            throw new NotImplementedException();
        }
    }
}
