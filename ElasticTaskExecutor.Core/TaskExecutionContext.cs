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
                () =>
                {
                    return new DaemonExecutor(
                        logger,
                        _executorRegistry, 
                        () => executionMonitoringInterval,
                        () => printMonitorInfo);
                });
            TryRegisterNewExecutorInternal(daemonExecutorMetadata);
#pragma warning disable 4014
            daemonExecutorMetadata.CreateNewTaskExecutor();
#pragma warning restore 4014
        }

        public bool TryRegisterNewExecutor(TaskExecutorMetadata metadata)
        {
            if (metadata.TaskExecutorTypeId == Constraint.DaemonExecutorId)
            {
                return false;
            }

            return TryRegisterNewExecutorInternal(metadata);
        }

        public bool TryRegisterNewExecutorInternal(TaskExecutorMetadata metadata)
        {
            HandleMetadataRegistration(metadata);
            return _executorRegistry.TryAdd(metadata.TaskExecutorTypeId, metadata);
        }

        public async Task<bool> TryUnRegisterExecutorAsync(int taskExecutorTypeId)
        {
            if (taskExecutorTypeId == Constraint.DaemonExecutorId)
            {
                return false;
            }

            if (_executorRegistry.TryRemove(taskExecutorTypeId, out var metadataInstance))
            {
                if (metadataInstance == null)
                {
                    return true;
                }

                while (true)
                {
                    var currentRunningTaskCnt = metadataInstance.GetExecutorCounter();
                    if (currentRunningTaskCnt <= 0)
                    {
                        if (PrintMonitorInfo)
                        {
                            _logger?.LogInfo(
                                $"All {metadataInstance.GetTaskExecutorIndex()} executor instance exited gracefully.");
                        }

                        break;
                    }

                    if (PrintMonitorInfo)
                    {
                        _logger?.LogInfo(
                            $"Waiting safe exit. {currentRunningTaskCnt} {metadataInstance.GetTaskExecutorIndex()} executor instances running");
                    }

                    await Task.Delay(ExitMonitoringInterval).ConfigureAwait(false);
                }

                HandleMetadataUnRegistration(metadataInstance);
                return true;
            }
            return false;
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

            foreach (var taskExecutorMetadata in _executorRegistry)
            {
                taskExecutorMetadata.Value.TaskManagerCancellationToken = null;
                taskExecutorMetadata.Value.GlobalApproveNewExecutorCreationCriteriaInContext = null;
            }

        }

        private void HandleMetadataRegistration(TaskExecutorMetadata metadata)
        {
            
            if (metadata.TaskManagerCancellationToken != null)
            {
                throw new Exception("Metadata was bind to a execution context");
            }
            metadata.TaskManagerCancellationToken = _cts;
            metadata.GlobalApproveNewExecutorCreationCriteriaInContext = GlobalApproveNewExecutorCreationCriteria;
        }

        private void HandleMetadataUnRegistration(TaskExecutorMetadata metadata)
        {
            if (metadata != null)
            {
                metadata.TaskManagerCancellationToken = null;
                metadata.GlobalApproveNewExecutorCreationCriteriaInContext = null;
            }
        }

        public Func<bool> GlobalApproveNewExecutorCreationCriteria = () => true;
        
        public void Dispose()
        {
            _cts?.Dispose();
        }
    }
}