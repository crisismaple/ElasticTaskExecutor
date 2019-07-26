namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Internal;
    using Newtonsoft.Json;

    public class TaskExecutionContext : IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly ConcurrentDictionary<int, TaskExecutorMetadata> _executorRegistry =
            new ConcurrentDictionary<int, TaskExecutorMetadata>();

        private readonly ILogger _logger;
        public TimeSpan ExecutionMonitoringInterval;
        public TimeSpan ExitMonitoringInterval;
        public bool PrintMonitorInfo;
        public TaskExecutionContext(ILogger logger, TimeSpan executionMonitoringInterval,
            TimeSpan exitMonitoringInterval, bool printMonitorInfo = false)
        {
            _logger = logger;
            ExecutionMonitoringInterval = executionMonitoringInterval;
            ExitMonitoringInterval = exitMonitoringInterval;
            PrintMonitorInfo = printMonitorInfo;
            var daemonExecutorMetadata = new DaemonExecutorMetadata(_logger,
                () => new DaemonExecutor(logger, _cts, _executorRegistry, () => executionMonitoringInterval,
                    () => printMonitorInfo));
            _executorRegistry.TryAdd(daemonExecutorMetadata.TaskExecutorTypeId, daemonExecutorMetadata);
#pragma warning disable 4014
            daemonExecutorMetadata.CreateNewTaskExecutor();
#pragma warning restore 4014
        }

        public bool TryRegisterNewExecutor(TaskExecutorMetadata metadata)
        {
            //Pass GlobalApproveNewExecutorCreationCriteria registered in execution context
            metadata.GlobalApproveNewExecutorCreationCriteriaInContext = GlobalApproveNewExecutorCreationCriteria;
            return _executorRegistry.TryAdd(metadata.TaskExecutorTypeId, metadata);
        }

        public async Task FinalizeAsync()
        {
            _cts.Cancel();
            while (true)
            {
                var currentRunningStatus = _executorRegistry.ToDictionary(kv => kv.Key,
                    kv => (kv.Value.TaskExecutorName, kv.Value.GetExecutorCounter()));
                var currentRunningTaskCnt = currentRunningStatus.Values.Select(m => m.Item2).Sum();
                if (currentRunningTaskCnt <= 0)
                {
                    if (PrintMonitorInfo)
                    {
                        _logger?.LogInfo("All executors exited gracefully.");
                    }
                    break;
                }

                if (PrintMonitorInfo)
                {
                    _logger?.LogInfo(
                        $"Waiting safe exit. Pending executor info: {currentRunningTaskCnt} task is running: {JsonConvert.SerializeObject(currentRunningStatus)}");
                }

                await Task.Delay(ExitMonitoringInterval).ConfigureAwait(false);
            }
        }

        public Func<bool> GlobalApproveNewExecutorCreationCriteria = () => true;
        
        public void Dispose()
        {
            _cts?.Dispose();
        }
    }
}