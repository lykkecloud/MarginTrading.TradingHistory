﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.TradingHistory.Core.Domain
{
    [JsonConverter(typeof(StringEnumConverter))]
    public static class ChannelTypes
    {
        public const string MarginTrading = "MarginTrading";
        public const string Monitor = "Monitor";
        
    }
}
