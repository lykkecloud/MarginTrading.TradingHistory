using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using MarginTrading.TradingHistory.Core.Domain;
using MarginTrading.TradingHistory.Core.Repositories;
using MarginTrading.TradingHistory.SqlRepositories.Entities;

namespace MarginTrading.TradingHistory.SqlRepositories
{
    public class TradesRepository : ITradesRepository
    {
        private const string TableName = "Trades";

        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 @"[OID] [bigint] NOT NULL IDENTITY (1,1) PRIMARY KEY,
[Id] [nvarchar](64) NOT NULL,
[ClientId] [nvarchar](64) NOT NULL,
[AccountId] [nvarchar](64) NOT NULL,
[OrderId] [nvarchar](64) NOT NULL,
[PositionId] [nvarchar] (64) NOT NULL,
[AssetPairId] [nvarchar] (64) NOT NULL,
[Type] [nvarchar] (64) NOT NULL,
[TradeTimestamp] [datetime] NOT NULL,
[Price] [float] NULL,
[Volume] [float] NULL,
CONSTRAINT IX_DealHistory1 NONCLUSTERED (OrderId, PositionId)
);";

        private readonly string _connectionString;
        private readonly ILog _log;

        private static readonly string GetColumns =
            string.Join(",", typeof(ITrade).GetProperties().Select(x => x.Name));

        private static readonly string GetFields =
            string.Join(",", typeof(ITrade).GetProperties().Select(x => "@" + x.Name));

        private static readonly string GetUpdateClause = string.Join(",",
            typeof(ITrade).GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        public TradesRepository(string connectionString, ILog log)
        {
            _connectionString = connectionString;
            _log = log;
            
            using (var conn = new SqlConnection(connectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(TradesRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }
        
        
        public async Task AddAsync(ITrade obj)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                try
                {
                    var entity = TradeEntity.Create(obj);
                    await conn.ExecuteAsync(
                        $"insert into {TableName} ({GetColumns}) values ({GetFields})", entity);
                }
                catch (Exception ex)
                {
                    var msg = $"Error {ex.Message} \n" +
                              "Entity <ITradeHistory>: \n" +
                              obj.ToJson();
                    
                    _log?.WriteWarning(nameof(TradesRepository), nameof(AddAsync), msg);
                    
                    throw new Exception(msg);
                }
            }
        }

        public async Task<ITrade> GetAsync(string tradeId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = $"SELECT * FROM {TableName} WHERE Id = @tradeId";
                var objects = await conn.QueryAsync<TradeEntity>(query, new {tradeId});
                
                return objects.FirstOrDefault();
            }
        }

        public async Task<IEnumerable<ITrade>> GetAsync(string orderId, string positionId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var clause = "WHERE 1=1 "
                             + (string.IsNullOrWhiteSpace(orderId) ? "" : " OrderId = @orderId")
                             + (string.IsNullOrWhiteSpace(positionId) ? "" : " PositionId = @positionId");
                
                var query = $"SELECT * FROM {TableName} {clause}";
                return await conn.QueryAsync<TradeEntity>(query, new {orderId, positionId});
            }
        }
    }
}
