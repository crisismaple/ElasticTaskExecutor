using System;
using System.Linq;
using System.Threading;
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
            Action<ElasticTaskExecutorHostServiceOptions> configurate)
        {
            var option = new ElasticTaskExecutorHostServiceOptions();
            configurate(option);
            return services.AddSingleton(serviceProvider =>
            {
                TaskExecutionContext context = null;
                if (option.EnableTaskPullerContext)
                {
                    context = serviceProvider.GetRequiredService<TaskExecutionContext>();
                }
                return (IHostedService) new ElasticTaskExecutorHostedService(context, option);
            });
        }

        public static IServiceCollection AddElasticTaskExecutor(
            this IServiceCollection services,
            Action<ElasticTaskExecutorConfigurator> configure)
        {
            var config = new ElasticTaskExecutorConfigurator();
            configure(config);
            return services.AddSingleton(
                serviceProvider => {
                    var context = new TaskExecutionContext(
                        serviceProvider.GetRequiredService<ILogger<TaskExecutionContext>>(),
                        config.ExecutionMonitoringInterval,
                        config.ExitMonitoringInterval,
                        config.MonitorInfoLogLevel);
                    config.TaskPullerMetadataCollection.ForEach(p =>
                        context.TryRegisterNewExecutorAsync(p, CancellationToken.None).Wait());
                    return context;
                    });
        }
    }
}