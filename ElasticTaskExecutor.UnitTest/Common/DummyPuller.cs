namespace ElasticTaskExecutor.UnitTest.Common
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Core.Common;

    public class DummyPuller : TaskPuller
    {
        public DummyPuller(ILogger logger) : base(logger)
        {
        }
        private readonly Random _seed = new Random();

        protected override async Task Execution(CancellationTokenSource cts)
        {
            ExecutorLogger.LogInfo($"Entering {nameof(Execution)}, cancellation source status {cts.IsCancellationRequested}");
            if (cts.IsCancellationRequested)
            {
                ExecutorLogger.LogInfo($"Existing {nameof(Execution)}, cancellation source status {cts.IsCancellationRequested}");
                return;
            }
            var sleepInterval = _seed.Next(1000, 2000);
            ExecutorLogger.LogInfo($"Sleeping {sleepInterval}ms in {nameof(Execution)}");
            await Task.Delay(sleepInterval).ConfigureAwait(false);
            ExecutorLogger.LogInfo($"Exiting {nameof(Execution)}, cancellation source status {cts.IsCancellationRequested}");
        }

        protected override bool ShouldTryToCreateNewPuller()
        {
            var result = _seed.Next() % 2 == 1;
            ExecutorLogger.LogInfo($"Return {result} from {nameof(ShouldTryToCreateNewPuller)}");
            return result;
        }

        protected override bool ShouldTryTerminateCurrentPuller()
        {
            var result = _seed.Next() % 2 == 1;
            ExecutorLogger.LogInfo($"Return {result} from {nameof(ShouldTryTerminateCurrentPuller)}");
            return result;
        }
    }
}