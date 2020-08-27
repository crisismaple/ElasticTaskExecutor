using Microsoft.Extensions.Logging;

namespace ElasticTaskExecutor.UnitTest.Common
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;

    public class DummyPuller : TaskPuller
    {
        private readonly ILogger logger;

        public DummyPuller(ILogger logger)
        {
            this.logger = logger;
            ExecutionStarting += o => logger.LogInformation($"Entering {o.Id} {nameof(Execution)}");
            ExecutionFinished += o => logger.LogInformation($"Exiting {o.Id} {nameof(Execution)}");
            ExecutionCancelled += o => logger.LogInformation($"Cancelled {o.Id} {nameof(Execution)}");

        }
        private readonly Random _seed = new Random();

        protected override async Task Execution(CancellationTokenSource cts)
        {
            cts.Token.ThrowIfCancellationRequested();
            var sleepInterval = _seed.Next(1000, 2000);
            logger.LogInformation($"Sleeping {sleepInterval}ms in {Id} {nameof(Execution)}");
            await Task.Delay(sleepInterval).ConfigureAwait(false);
        }

        protected override bool ShouldTryToCreateNewPuller()
        {
            var result = _seed.Next() % 2 == 1;
            logger.LogInformation($"Return {result} from {Id}");
            return result;
        }

        protected override bool ShouldTryTerminateCurrentPuller()
        {
            var result = _seed.Next() % 2 == 1;
            logger.LogInformation($"Return {result} from {Id}");
            return result;
        }
    }
}