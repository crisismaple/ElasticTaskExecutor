namespace ElasticTaskExecutor.UnitTest.Common
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Core.Common;

    public class DummyExecutor : TaskExecutor
    {
        public DummyExecutor(ILogger logger) : base(logger)
        {
        }
        private readonly Random _seed = new Random();

        protected override async Task Execution(CancellationTokenSource cts)
        {
            Logger.LogInfo($"Entering {nameof(Execution)}, cancellation source status {cts.IsCancellationRequested}");
            if (cts.IsCancellationRequested)
            {
                Logger.LogInfo($"Existing {nameof(Execution)}, cancellation source status {cts.IsCancellationRequested}");
                return;
            }
            var sleepInterval = _seed.Next(1000, 2000);
            Logger.LogInfo($"Sleeping {sleepInterval}ms in {nameof(Execution)}");
            await Task.Delay(sleepInterval).ConfigureAwait(false);
            Logger.LogInfo($"Exiting {nameof(Execution)}, cancellation source status {cts.IsCancellationRequested}");
        }

        protected override bool ShouldTryToCreateNewExecutor()
        {
            var result = _seed.Next() % 2 == 1;
            Logger.LogInfo($"Return {result} from {nameof(ShouldTryToCreateNewExecutor)}");
            return result;
        }

        protected override bool ShouldTryTerminateCurrentExecutor()
        {
            var result = _seed.Next() % 2 == 1;
            Logger.LogInfo($"Return {result} from {nameof(ShouldTryTerminateCurrentExecutor)}");
            return result;
        }
    }
}