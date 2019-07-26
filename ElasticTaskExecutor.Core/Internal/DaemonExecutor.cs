namespace ElasticTaskExecutor.Core.Internal
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    internal sealed class DaemonExecutor : TaskExecutor
    {
        private readonly ConcurrentDictionary<int, TaskExecutorMetadata> _executorRegistry;
        private readonly Func<TimeSpan> _executionMonitoringIntervalFunc;
        private readonly Func<bool> _printMonitorInfoFunc;

        public DaemonExecutor(ILogger logger,
            CancellationTokenSource taskManagerCancellationToken,
            ConcurrentDictionary<int, TaskExecutorMetadata> executorRegistry,
            Func<TimeSpan> executionMonitoringIntervalFunc,
            Func<bool> printMonitorInfoFunc) :
            base(taskManagerCancellationToken, logger)
        {
            _executorRegistry = executorRegistry;
            _executionMonitoringIntervalFunc = executionMonitoringIntervalFunc;
            _printMonitorInfoFunc = printMonitorInfoFunc;
        }

        protected override async Task Execution(CancellationTokenSource cts)
        {
            var currentlyEnabledTaskExecutors = _executorRegistry.Where(kv => kv.Key!= Constraint.DaemonExecutorId && kv.Value.IsEnabled).Select(kv => kv.Value).ToList();
            var printMonitorInfo = _printMonitorInfoFunc();
            if (printMonitorInfo)
            {
                Logger?.LogInfo(
                    $"Currently {currentlyEnabledTaskExecutors.Count} type executors are enabled: {string.Join("; ", currentlyEnabledTaskExecutors.Select(x => x.GetTaskExecutorIndex()))}");
            }

            currentlyEnabledTaskExecutors.ForEach(t =>
            {
                var counter = t.GetExecutorCounter();
                if (!cts.IsCancellationRequested && counter == 0)
                {
                    var minExecutorCnt = (int)t.GetMinExecutorCount();
                    Logger?.LogInfo($"Start to create {minExecutorCnt} executors for {t.GetTaskExecutorIndex()}");
                    Parallel.ForEach(Enumerable.Range(0, minExecutorCnt),
                        i => { Task.Factory.StartNew(async () => await t.CreateNewTaskExecutor().ConfigureAwait(false)); });
                }

                if (printMonitorInfo)
                {
                    Logger?.LogInfo($"There are {counter} executors running for {t.GetTaskExecutorIndex()}");
                }
            });

            var monitorTimespan = _executionMonitoringIntervalFunc();
            if (printMonitorInfo)
            {
                Logger?.LogInfo(
                    $"Current monitor timespan is set to {monitorTimespan:c}");
            }

            try
            {
                await Task.Delay(monitorTimespan, cts.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                //Ignore exception here
            }
        }

        protected override bool ShouldTryToCreateNewExecutor()
        {
            return false;
        }

        protected override bool ShouldTryTerminateCurrentExecutor()
        {
            return false;
        }

        public override bool IsExecutorEnabled()
        {
            return true;
        }
    }
}