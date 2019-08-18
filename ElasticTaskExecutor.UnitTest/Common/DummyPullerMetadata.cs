namespace ElasticTaskExecutor.UnitTest.Common
{
    using Core;
    using Core.Common;

    public class DummyPullerMetadata : TaskPullerMetadata
    {
        public DummyPullerMetadata(ILogger logger,
            int taskExecutorTypeId,
            string taskExecutorName)
        {
            TaskExecutorName = taskExecutorName;
            TaskExecutorTypeId = taskExecutorTypeId;
            Logger = logger;
        }
        
        public bool IsEnabled { private get; set; }

        public override bool IsExecutorEnabled => IsEnabled;

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

        protected override TaskPuller ExecutorActivator()
        {
            return new DummyPuller(Logger);
        }
    }
}