namespace ElasticTaskExecutor.UnitTest
{
    using System;
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
                context.TryRegisterNewExecutor(dummyMetadata);

                await Task.Delay(5000).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                await Task.Delay(5000).ConfigureAwait(false);

                dummyMetadata.IsEnabled = false;

                await Task.Delay(5000).ConfigureAwait(false);
                await context.FinalizeAsync().ConfigureAwait(false);
            }
            
        }
    }
}
