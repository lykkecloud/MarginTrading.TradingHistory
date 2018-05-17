﻿using Lykke.SettingsReader.Attributes;

namespace MarginTrading.TradingHistory.Settings.ServiceSettings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
    }
}
