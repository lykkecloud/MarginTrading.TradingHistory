﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.TradingHistory.Client.Models
{
    public class ActivityContract
    {
        public string AccountId { get; set; }
        public DateTime Timestamp { get; set; }
        public ActivityTypeContract Type { get; set; }
        public string Comment { get; set; }

        public string Instrument { get; set; }
        public string OrderId { get; set; }
        public string PositionId { get; set; }
    }
}
