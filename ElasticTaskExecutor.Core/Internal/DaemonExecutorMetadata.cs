namespace ElasticTaskExecutor.Core.Internal
{
    using System;
    using Common;

    internal class DaemonExecutorMetadata : TaskPullerMetadata
    {
        private readonly Func<DaemonExecutor> _daemonExecutorConstructor;

        public DaemonExecutorMetadata(ILogger logger, Func<DaemonExecutor> daemonExecutorConstructor)
        {
            Logger = logger;
            _daemonExecutorConstructor = daemonExecutorConstructor;
            TaskExecutorName = nameof(DaemonExecutor);
        }

        public override int TaskExecutorTypeId => Constraint.DaemonExecutorId;
        protected override ILogger Logger { get; }

        public override long GetMinExecutorCount()
        {
            return 1;
        }

        public override long GetMaxExecutorCount()
        {
            return 1;
        }
        
        protected override TaskPuller ExecutorActivator()
        {
            return _daemonExecutorConstructor();
        }
    }
}