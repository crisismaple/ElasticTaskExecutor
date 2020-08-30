using Microsoft.Extensions.Logging;

namespace ElasticTaskExecutor.UnitTest.Common
{
    using System;
    using Core;

    public class DummyPullerMetadata : TaskPullerMetadata
    {
        public DummyPullerMetadata(ILogger logger,
            int taskExecutorTypeId,
            string taskExecutorName)
        {
            TaskExecutorName = taskExecutorName;
            TaskPullerTypeId = taskExecutorTypeId;
            Logger = logger;
        }
        
        public bool IsEnabled { private get; set; }

        public override bool IsExecutorEnabled => IsEnabled;

        protected override int TaskPullerTypeId { get; }

        public override TimeSpan? ExecutionTimeout => null;

        protected ILogger Logger { get; }
        public override long GetMinExecutorCount()
        {
            return 2;
        }

        public override long GetMaxExecutorCount()
        {
            return 3;
        }

        protected override TaskPuller ExecutorActivator()
        {
            return new DummyPuller(Logger);
        }
    }
}