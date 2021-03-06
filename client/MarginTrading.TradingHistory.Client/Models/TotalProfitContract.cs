// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.TradingHistory.Client.Models
{
    /// <summary>
    /// The deals total profit response contract
    /// </summary>
    [PublicAPI]
    public class TotalProfitContract
    {
        /// <summary>
        /// The value of total PnL
        /// </summary>
        public decimal Value { get; set; }

        public static TotalProfitContract Empty() => new TotalProfitContract {Value = 0};
    }
}
