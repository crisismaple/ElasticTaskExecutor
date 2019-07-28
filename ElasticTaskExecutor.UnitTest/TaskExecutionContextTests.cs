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
            var logger = new DummyLogger();
            var monitorTimespan = TimeSpan.FromSeconds(10);
            var exitTimespan = TimeSpan.FromSeconds(5);
            var dummyMetadata = new DummyExecutorMetadata(logger, 0, nameof(DummyExecutor));

            using (var context = new TaskExecutionContext(logger, monitorTimespan, exitTimespan, true))
            {
                await context.TryRegisterNewExecutorAsync(dummyMetadata, CancellationToken.None).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                dummyMetadata.IsEnabled = false;
                logger.LogInfo("Disabled");
                await Task.Delay(5000).ConfigureAwait(false);

                dummyMetadata.IsEnabled = true;
                logger.LogInfo("Re-Enabled");
                await Task.Delay(20000).ConfigureAwait(false);
                await context.FinalizeAsync().ConfigureAwait(false);
            }
            
        }

        [TestMethod]
        public async Task TestMethod2()
        {
            var logger = new DummyLogger();
            var monitorTimespan = TimeSpan.FromSeconds(1);
            var exitTimespan = TimeSpan.FromSeconds(5);
            var dummyMetadata1 = new DummyExecutorMetadata(logger, 0, nameof(DummyExecutor) + "0");
            var dummyMetadata2 = new DummyExecutorMetadata(logger, 1, nameof(DummyExecutor) + "1");

            using (var context = new TaskExecutionContext(logger, monitorTimespan, exitTimespan, true))
            {
                var addTask1 =context.TryRegisterNewExecutorAsync(dummyMetadata1, CancellationToken.None);
                var addTask2 =context.TryRegisterNewExecutorAsync(dummyMetadata2, CancellationToken.None);

                await Task.WhenAll(addTask1, addTask2).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);
                await Task.Delay(5000).ConfigureAwait(false);

                dummyMetadata1.IsEnabled = false;
                logger.LogInfo("Disabled 0");
                await Task.Delay(5000).ConfigureAwait(false);
                dummyMetadata1.IsEnabled = true;
                logger.LogInfo("Re-Enabled 0");
                await Task.Delay(5000).ConfigureAwait(false);

                await context.TryUnRegisterExecutorAsync(1, CancellationToken.None).ConfigureAwait(false);
                dummyMetadata2.IsEnabled = false;
                logger.LogInfo("Disabled 1");
                await Task.Delay(5000).ConfigureAwait(false);
                dummyMetadata2.IsEnabled = true;
                logger.LogInfo("Re-Enabled 1");
                await Task.Delay(5000).ConfigureAwait(false);
                await context.FinalizeAsync().ConfigureAwait(false);
            }

        }
    }
}
