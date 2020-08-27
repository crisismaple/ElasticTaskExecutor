using System;
using ElasticTaskExecutor.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElasticTaskExecutor.Extensions
{
    public static class ElasticTaskExecutorServiceCollectionExtensions
    {
        public static IServiceCollection AddElasticTaskExecutorServer(
            this IServiceCollection services,
            bool waitForExecutorTaskComplete = true)
        {
            return services.AddSingleton(serviceProvider =>
            {
                var context = serviceProvider.GetRequiredService<TaskExecutionContext>();
                return (IHostedService) new ElasticTaskExecutorHostedService(context, waitForExecutorTaskComplete);
            });
        }

        public static IServiceCollection AddElasticTaskExecutor(
            this IServiceCollection services,
            TimeSpan executionMonitoringInterval,
            TimeSpan exitMonitoringInterval,
            LogLevel monitorInfoLogLevel = LogLevel.Information)
        {
            return services.AddSingleton(
                serviceProvider => new TaskExecutionContext(
                    serviceProvider.GetRequiredService<ILogger<TaskExecutionContext>>(),
                    executionMonitoringInterval,
                    exitMonitoringInterval,
                    monitorInfoLogLevel));
        }
    }
}