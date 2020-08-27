using Microsoft.Extensions.Logging;

namespace ElasticTaskExecutor.Core.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    internal sealed class DaemonExecutor : TaskPuller
    {
        private readonly Dictionary<int, TaskPullerMetadata> _executorRegistry;
        private readonly Func<TimeSpan> _executionMonitoringIntervalFunc;
        private readonly Func<LogLevel> _monitorInfoLogLevel;

        public DaemonExecutor(ILogger executorLogger,
            Dictionary<int, TaskPullerMetadata> executorRegistry,
            Func<TimeSpan> executionMonitoringIntervalFunc,
            Func<LogLevel> monitorInfoLogLevel)
        {
            _executorRegistry = executorRegistry;
            _executionMonitoringIntervalFunc = executionMonitoringIntervalFunc;
            _monitorInfoLogLevel = monitorInfoLogLevel;
            ExecutorLogger = executorLogger;
            this.ExecutionStarting += o => ExecutorLogger?.LogInformation($"{nameof(DaemonExecutor)} {Id.ToString()} started");
            this.ExecutionFinished += o => ExecutorLogger?.LogInformation($"{nameof(DaemonExecutor)} {Id.ToString()} finished");
        }

        private ILogger ExecutorLogger { get; }

        protected override async Task Execution(CancellationTokenSource cts)
        {
            var currentlyAttachedTaskExecutors = _executorRegistry.Where(kv => kv.Key != Constraint.DaemonExecutorId)
                .Select(kv => (ExecutorMetadata: kv.Value, IsExecutorEnabled: kv.Value.IsExecutorEnabled)).ToList();
            var monitorInfoLogLevel = _monitorInfoLogLevel();

            ExecutorLogger?.Log(monitorInfoLogLevel,
                $"Currently {currentlyAttachedTaskExecutors.Count} type executors are attached.");
            foreach (var currentlyAttachedTaskExecutor in currentlyAttachedTaskExecutors)
            {
                ExecutorLogger?.Log(monitorInfoLogLevel,
                    $"{currentlyAttachedTaskExecutor.ExecutorMetadata.GetTaskExecutorIndex()} -> {(currentlyAttachedTaskExecutor.IsExecutorEnabled ? "Enabled" : "Disabled")}");
            }


            currentlyAttachedTaskExecutors.ForEach(t =>
            {
                var counter = t.ExecutorMetadata.GetExecutorCounter();
                ExecutorLogger?.Log(monitorInfoLogLevel,
                    $"There are {counter} executors running for {t.ExecutorMetadata.GetTaskExecutorIndex()}({(t.IsExecutorEnabled ? "Enabled" : "Disabled")})");

                if (!cts.IsCancellationRequested && t.IsExecutorEnabled && counter == 0)
                {
                    var minExecutorCnt = (int) t.ExecutorMetadata.GetMinExecutorCount();
                    if (minExecutorCnt == 0 && t.ExecutorMetadata.ShouldBeReactivate)
                    {
                        //At least create 1 instance for suspended executors
                        minExecutorCnt += 1;
                    }

                    if (minExecutorCnt > 0)
                    {
                        ExecutorLogger?.LogInformation(
                            $"Start to create {minExecutorCnt} executors for {t.ExecutorMetadata.GetTaskExecutorIndex()}");
                        Parallel.ForEach(Enumerable.Range(0, minExecutorCnt),
                            i =>
                            {
                                Task.Factory.StartNew(async () =>
                                    await t.ExecutorMetadata.CreateNewTaskExecutor(cts.Token).ConfigureAwait(false));
                            });
                    }
                    else
                    {
                        ExecutorLogger?.LogInformation(
                            $"Skip to create executors for suspended executor {t.ExecutorMetadata.GetTaskExecutorIndex()}");
                    }
                }
            });

            var monitorTimespan = _executionMonitoringIntervalFunc();
            ExecutorLogger?.Log(monitorInfoLogLevel,
                $"Current monitor timespan is set to {monitorTimespan:c}");
            try
            {
                await Task.Delay(monitorTimespan, cts.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                //Ignore exception here
            }
        }

        protected override bool ShouldTryToCreateNewPuller()
        {
            return false;
        }

        protected override bool ShouldTryTerminateCurrentPuller()
        {
            return false;
        }
    }
}