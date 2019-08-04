namespace ElasticTaskExecutor.Core.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    internal sealed class DaemonExecutor : TaskExecutor
    {
        private readonly Dictionary<int, TaskExecutorMetadata> _executorRegistry;
        private readonly Func<TimeSpan> _executionMonitoringIntervalFunc;
        private readonly Func<bool> _printMonitorInfoFunc;

        public DaemonExecutor(ILogger logger,
            Dictionary<int, TaskExecutorMetadata> executorRegistry,
            Func<TimeSpan> executionMonitoringIntervalFunc,
            Func<bool> printMonitorInfoFunc) :
            base(logger)
        {
            _executorRegistry = executorRegistry;
            _executionMonitoringIntervalFunc = executionMonitoringIntervalFunc;
            _printMonitorInfoFunc = printMonitorInfoFunc;
        }

        protected override async Task Execution(CancellationTokenSource cts)
        {
            var currentlyAttachedTaskExecutors = _executorRegistry.Where(kv => kv.Key!= Constraint.DaemonExecutorId).Select(kv => kv.Value).ToList();
            var printMonitorInfo = _printMonitorInfoFunc();
            if (printMonitorInfo)
            {
                Logger?.LogInfo(
                    $"Currently {currentlyAttachedTaskExecutors.Count} type executors are attached.");
                foreach (var currentlyAttachedTaskExecutor in currentlyAttachedTaskExecutors)
                {
                    Logger?.LogInfo(
                        $"{currentlyAttachedTaskExecutor.GetTaskExecutorIndex()} -> {(currentlyAttachedTaskExecutor.IsEnabled ? "Enabled" : "Disabled")}");
                }
            }

            currentlyAttachedTaskExecutors.ForEach(t =>
            {
                var counter = t.GetExecutorCounter();
                if (printMonitorInfo)
                {
                    Logger?.LogInfo($"There are {counter} executors running for {t.GetTaskExecutorIndex()}({(t.IsEnabled ? "Enabled" : "Disabled")})");
                }
                if (!cts.IsCancellationRequested && t.IsEnabled && counter == 0)
                {
                    var minExecutorCnt = (int)t.GetMinExecutorCount();
                    if (minExecutorCnt == 0 && t.ShouldBeReactivate())
                    {
                        //At least create 1 instance for suspended executors
                        minExecutorCnt += 1;
                    }

                    if (minExecutorCnt > 0)
                    {
                        Logger?.LogInfo($"Start to create {minExecutorCnt} executors for {t.GetTaskExecutorIndex()}");
                        Parallel.ForEach(Enumerable.Range(0, minExecutorCnt),
                            i => { Task.Factory.StartNew(async () => await t.CreateNewTaskExecutor().ConfigureAwait(false)); });
                    }
                    else
                    {
                        Logger?.LogInfo($"Skip to create executors for suspended executor {t.GetTaskExecutorIndex()}");
                    }
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
    }
}