﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using MarginTrading.TradingHistory.Core;
using MarginTrading.TradingHistory.Core.Domain;
using MarginTrading.TradingHistory.Core.Extensions;
using MarginTrading.TradingHistory.Core.Repositories;
using MarginTrading.TradingHistory.SqlRepositories.Entities;

namespace MarginTrading.TradingHistory.SqlRepositories
{
    public class PositionsHistorySqlRepository : IPositionsHistoryRepository
    {
        private const string TableName = "PositionsHistory";

        private readonly string _connectionString;
        private readonly ILog _log;

        private static readonly string GetColumns =
            string.Join(",", typeof(PositionsHistoryEntity).GetProperties().Select(x => x.Name));

        private static readonly string GetFields =
            string.Join(",", typeof(PositionsHistoryEntity).GetProperties().Select(x => "@" + x.Name));

        public PositionsHistorySqlRepository(string connectionString, ILog log)
        {
            _connectionString = connectionString;
            _log = log;
            
            connectionString.InitializeSqlObject("dbo.Deals.sql", log);
            connectionString.InitializeSqlObject("dbo.DealCommissionParams.sql", log);
            connectionString.InitializeSqlObject("dbo.SP_UpdateDealCommissionParamsOnDeal.sql", log);
            connectionString.InitializeSqlObject("dbo.PositionHistory.sql", log);
        }

        public async Task AddAsync(IPositionHistory positionHistory, IDeal deal)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                if (conn.State == ConnectionState.Closed)
                {
                    await conn.OpenAsync();
                }
                var transaction = conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    var positionEntity = PositionsHistoryEntity.Create(positionHistory);
                    await conn.ExecuteAsync($"insert into {TableName} ({GetColumns}) values ({GetFields})",
                        positionEntity,
                        transaction);

                    if (deal != null)
                    {
                        var entity = DealEntity.Create(deal);

                        await conn.ExecuteAsync($@"INSERT INTO [dbo].[Deals] 
({string.Join(",", DealsSqlRepository.DealInsertColumns)}) 
VALUES (@{string.Join(",@", DealsSqlRepository.DealInsertColumns)})",
                            new
                            {
                                DealId = entity.DealId,
                                Created = entity.Created,
                                AccountId = entity.AccountId,
                                AssetPairId = entity.AssetPairId,
                                OpenTradeId = entity.OpenTradeId,
                                OpenOrderType = entity.OpenOrderType,
                                OpenOrderVolume = entity.OpenOrderVolume,
                                OpenOrderExpectedPrice = entity.OpenOrderExpectedPrice,
                                CloseTradeId = entity.CloseTradeId,
                                CloseOrderType = entity.CloseOrderType,
                                CloseOrderVolume = entity.CloseOrderVolume,
                                CloseOrderExpectedPrice = entity.CloseOrderExpectedPrice,
                                Direction = entity.Direction,
                                Volume = entity.Volume,
                                Originator = entity.Originator,
                                OpenPrice = entity.OpenPrice,
                                OpenFxPrice = entity.OpenFxPrice,
                                ClosePrice = entity.ClosePrice,
                                CloseFxPrice = entity.CloseFxPrice,
                                Fpl = entity.Fpl,
                                PnlOfTheLastDay = entity.PnlOfTheLastDay,
                                AdditionalInfo = entity.AdditionalInfo,
                            },
                            transaction);

                        await conn.ExecuteAsync("INSERT INTO [dbo].[DealCommissionParams] (DealId) VALUES (@DealId)",
                            new {deal.DealId},
                            transaction);
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    var msg = $"Error {ex.Message} \n" +
                              $"Entity <{nameof(IPositionHistory)}>: \n" +
                              positionHistory.ToJson() + " \n" +
                              $"Entity <{nameof(IDeal)}>: \n" +
                              deal?.ToJson();
                    
                    await _log?.WriteErrorAsync(nameof(PositionsHistorySqlRepository), nameof(AddAsync), 
                        new Exception(msg));
                    
                    throw;
                }
            }
            
            if (deal != null)
            {
#pragma warning disable 4014
                Task.Run(async () =>
#pragma warning restore 4014
                {
                    try
                    {
                        using (var conn = new SqlConnection(_connectionString))
                        {
                            await conn.ExecuteAsync("[dbo].[SP_UpdateDealCommissionParamsOnDeal]",
                                new
                                {
                                    DealId = deal.DealId,
                                    OpenTradeId = deal.OpenTradeId,
                                    OpenOrderVolume = deal.OpenOrderVolume,
                                    CloseTradeId = deal.CloseTradeId,
                                    CloseOrderVolume = deal.CloseOrderVolume,
                                    Volume = deal.Volume,
                                },
                                commandType: CommandType.StoredProcedure);
                        }
                    }
                    catch (Exception exception)
                    {
                        await _log?.WriteErrorAsync(nameof(PositionsHistorySqlRepository), nameof(AddAsync), 
                            new Exception($"Failed to calculate commissions for the deal {deal.DealId}, skipping.", 
                                exception));
                    }        
                });
            }
        }

        public async Task<List<IPositionHistory>> GetAsync(string accountId, string assetPairId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var whereClause = "Where 1=1 " +
                                  (string.IsNullOrEmpty(accountId) ? "" : " And AccountId = @accountId") +
                                  (string.IsNullOrEmpty(assetPairId) ? "" : " And AssetPairId = @assetPairId");

                var query = $"SELECT * FROM {TableName} {whereClause}";
                var objects = await conn.QueryAsync<PositionsHistoryEntity>(query, new {accountId, assetPairId});
                
                return objects.Cast<IPositionHistory>().ToList();
            }
        }

        public async Task<PaginatedResponse<IPositionHistory>> GetByPagesAsync(string accountId, string assetPairId, 
            int? skip = null, int? take = null)
        {
            var whereClause = " WHERE 1=1 "
                              + (string.IsNullOrWhiteSpace(accountId) ? "" : " AND AccountId=@accountId")
                              + (string.IsNullOrWhiteSpace(assetPairId) ? "" : " AND AssetPairId=@assetPairId");
            
            using (var conn = new SqlConnection(_connectionString))
            {
                var paginationClause = $" ORDER BY [Oid] OFFSET {skip ?? 0} ROWS FETCH NEXT {PaginationHelper.GetTake(take)} ROWS ONLY";
                var gridReader = await conn.QueryMultipleAsync(
                    $"SELECT * FROM {TableName} {whereClause} {paginationClause}; SELECT COUNT(*) FROM {TableName} {whereClause}",
                    new {accountId,  assetPairId});
                var positionsHistoryEntities = (await gridReader.ReadAsync<PositionsHistoryEntity>()).ToList();
                var totalCount = await gridReader.ReadSingleAsync<int>();
             
                return new PaginatedResponse<IPositionHistory>(
                    contents: positionsHistoryEntities, 
                    start: skip ?? 0, 
                    size: positionsHistoryEntities.Count, 
                    totalSize: totalCount
                );
            }
        }

        public async Task<List<IPositionHistory>> GetAsync(string id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = $"SELECT * FROM {TableName} Where Id = @id";
                var objects = await conn.QueryAsync<PositionsHistoryEntity>(query, new {id});
                
                return objects.Cast<IPositionHistory>().ToList();
            }
        }
    }
}
