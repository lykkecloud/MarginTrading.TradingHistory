using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.TradingHistory.Client.Common;
using MarginTrading.TradingHistory.Client.Models;
using Refit;

namespace MarginTrading.TradingHistory.Client
{
    /// <summary>
    /// API for the deals
    /// </summary>
    [PublicAPI]
    public interface IDealsApi
    {
        /// <summary> 
        /// Get deals with optional filtering 
        /// </summary> 
        [Get("/api/deals")] 
        Task<List<DealContract>> List( 
            [Query, CanBeNull] string accountId, 
            [Query, CanBeNull] string instrument);
        
        /// <summary> 
        /// Get deals with optional filtering and pagination 
        /// </summary> 
        [Get("/api/deals/by-pages")] 
        Task<PaginatedResponseContract<DealContract>> ListByPages( 
            [Query, CanBeNull] string accountId, [Query, CanBeNull] string instrument,
            [Query, CanBeNull] int? skip = null, [Query, CanBeNull] int? take = null);
        
        /// <summary>
        /// Get deal by Id
        /// </summary>
        [Get("/api/deals/{dealId}")]
        Task<DealContract> ById([NotNull] string dealId);
    }
}
