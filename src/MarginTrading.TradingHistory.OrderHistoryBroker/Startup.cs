﻿using Autofac;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.TradingHistory.AzureRepositories;
using MarginTrading.TradingHistory.BrokerBase;
using MarginTrading.TradingHistory.BrokerBase.Settings;
using MarginTrading.TradingHistory.Core;
using MarginTrading.TradingHistory.Core.Repositories;
using MarginTrading.TradingHistory.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.TradingHistory.OrderHistoryBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        public Startup(IHostingEnvironment env) : base(env)
        {
        }

        protected override string ApplicationName => "OrderHistoryBroker";

        protected override void RegisterCustomServices(IServiceCollection services, ContainerBuilder builder, IReloadingManager<Settings> settings, ILog log)
        {
            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();
            
            if (settings.CurrentValue.Db.StorageMode == StorageMode.Azure)
            {
                builder.RegisterInstance(AzureRepoFactories.MarginTrading.CreateOrdersHistoryRepository(
                        settings.Nested(s => s.Db.ConnString), log, new ConvertService()))
                    .As<IOrdersHistoryRepository>();
                builder.RegisterInstance(AzureRepoFactories.MarginTrading.CreateTradesHistoryRepository(
                        settings.Nested(s => s.Db.ConnString), log, new ConvertService()))
                    .As<ITradesRepository>();
            }
            else if (settings.CurrentValue.Db.StorageMode == StorageMode.SqlServer)
            {
                builder.RegisterInstance(new SqlRepositories.OrdersHistorySqlRepository(
                        settings.CurrentValue.Db.ConnString, log))
                    .As<IOrdersHistoryRepository>();
                builder.RegisterInstance(new SqlRepositories.TradesSqlRepository(
                        settings.CurrentValue.Db.ConnString, log))
                    .As<ITradesRepository>();
            }
        }
    }
}
