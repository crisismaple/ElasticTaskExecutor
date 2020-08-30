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
            Action<IServiceProvider, ElasticTaskExecutorHostServiceOptions> configurate)
        {
            return services.AddSingleton(serviceProvider =>
            {
                var option = new ElasticTaskExecutorHostServiceOptions();
                configurate(serviceProvider, option);
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
            Action<IServiceProvider, ElasticTaskExecutorConfigurator> configure)
        {
            return services.AddSingleton(
                serviceProvider => {
                    var config = new ElasticTaskExecutorConfigurator();
                    configure(serviceProvider, config);
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