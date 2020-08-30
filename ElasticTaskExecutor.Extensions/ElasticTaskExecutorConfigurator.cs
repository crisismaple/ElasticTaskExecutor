using ElasticTaskExecutor.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElasticTaskExecutor.Extensions
{
    public class ElasticTaskExecutorConfigurator
    {
        public TimeSpan ExecutionMonitoringInterval { get; set; } = TimeSpan.FromSeconds(15);
        public TimeSpan ExitMonitoringInterval { get; set; } = TimeSpan.FromSeconds(5);
        public LogLevel MonitorInfoLogLevel { get; set; } = LogLevel.Information;

        internal List<TaskPullerMetadata> TaskPullerMetadataCollection { get; set; } = new List<TaskPullerMetadata>();

        public void AddPullerMetadata(TaskPullerMetadata metadata)
        {
            TaskPullerMetadataCollection.Add(metadata);
        }
    }
}
