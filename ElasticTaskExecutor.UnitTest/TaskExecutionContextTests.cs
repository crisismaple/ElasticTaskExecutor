using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ElasticTaskExecutor.UnitTest
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TaskExecutionContextTests
    {
        
        [TestMethod]
        public async Task TestMethod1()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(l =>
                {
                    l.AddConsole();
                })
                .BuildServiceProvider();
            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<TaskExecutionContextTests>();
            var monitorTimespan = TimeSpan.FromSeconds(10);
            var exitTimespan = TimeSpan.FromSeconds(5);
            var dummyMetadata = new DummyPullerMetadata(logger, 0, nameof(DummyPuller));


            var subscription = TaskSubscriberMetadata<long>.CreateNewSubscription(
                10,
                () => new DummySubscriber<long>(), TimeSpan.FromMinutes(30));


            using (var context = new TaskExecutionContext(logger, monitorTimespan, exitTimespan, LogLevel.Debug, true))
            {
                await context.TryRegisterNewExecutorAsync(dummyMetadata, CancellationToken.None).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                dummyMetadata.IsEnabled = false;
                logger.LogInformation("Disabled");
                await Task.Delay(5000).ConfigureAwait(false);

                dummyMetadata.IsEnabled = true;
                logger.LogInformation("Re-Enabled");
                await Task.Delay(20000).ConfigureAwait(false);
                await context.FinalizeAsync(new CancellationToken()).ConfigureAwait(false);
            }
            
        }

        [TestMethod]
        public async Task TestMethod2()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(l =>
                {
                    l.AddConsole();
                })
                .BuildServiceProvider();
            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<TaskExecutionContextTests>();
            var monitorTimespan = TimeSpan.FromSeconds(1);
            var exitTimespan = TimeSpan.FromSeconds(5);
            var dummyMetadata1 = new DummyPullerMetadata(logger, 0, nameof(DummyPuller) + "0");
            var dummyMetadata2 = new DummyPullerMetadata(logger, 1, nameof(DummyPuller) + "1");

            using (var context = new TaskExecutionContext(logger, monitorTimespan, exitTimespan, LogLevel.Debug, true))
            {
                var addTask1 =context.TryRegisterNewExecutorAsync(dummyMetadata1, CancellationToken.None);
                var addTask2 =context.TryRegisterNewExecutorAsync(dummyMetadata2, CancellationToken.None);

                await Task.WhenAll(addTask1, addTask2).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);
                await Task.Delay(5000).ConfigureAwait(false);

                dummyMetadata1.IsEnabled = false;
                logger.LogInformation("Disabled 0");
                await Task.Delay(5000).ConfigureAwait(false);
                dummyMetadata1.IsEnabled = true;
                logger.LogInformation("Re-Enabled 0");
                await Task.Delay(5000).ConfigureAwait(false);

                await context.TryUnRegisterExecutorAsync(1, CancellationToken.None).ConfigureAwait(false);
                dummyMetadata2.IsEnabled = false;
                logger.LogInformation("Disabled 1");
                await Task.Delay(5000).ConfigureAwait(false);
                dummyMetadata2.IsEnabled = true;
                logger.LogInformation("Re-Enabled 1");
                await Task.Delay(5000).ConfigureAwait(false);
                await context.FinalizeAsync(new CancellationToken()).ConfigureAwait(false);
            }

        }
    }
}
