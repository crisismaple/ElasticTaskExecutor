namespace ElasticTaskExecutor.UnitTest.Common
{
    using System;
    using Core;
    using Core.Common;

    public class DummyExecutorMetadata : TaskExecutorMetadata
    {
        public DummyExecutorMetadata(ILogger logger,
            int taskExecutorTypeId,
            string taskExecutorName)
        {
            TaskExecutorName = taskExecutorName;
            IsEnabled = true;
            TaskExecutorTypeId = taskExecutorTypeId;
            Logger = logger;
        }
        
        public override int TaskExecutorTypeId { get; }
        protected override ILogger Logger { get; }
        public override long GetMinExecutorCount()
        {
            return 2;
        }

        public override long GetMaxExecutorCount()
        {
            return 3;
        }

        public override TimeSpan? GetExecutionTimeout()
        {
            return null;
        }

        protected override TaskExecutor ExecutorActivator()
        {
            return new DummyExecutor(Logger);
        }
    }
}