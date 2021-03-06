﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MarginTrading.TradingHistory.Client.Models
{
    public class OrderEventsFilterRequest
    {
        public string AccountId { get; set; }

        public string AssetPairId { get; set; }

        public List<OrderStatusContract> Statuses { get; set; }

        public string ParentOrderId { get; set; }

        public DateTime? CreatedTimeStart { get; set; }

        public DateTime? CreatedTimeEnd { get; set; }

        public DateTime? ModifiedTimeStart { get; set; }

        public DateTime? ModifiedTimeEnd { get; set; }

        public List<OrderTypeContract> OrderTypes { get; set; }

        public List<OriginatorTypeContract> OriginatorTypes { get; set; }

        public OrderEventsRequestType RequestType { get; set; } = OrderEventsRequestType.Default;
    }
}
