namespace ElasticTaskExecutor.Core.Internal
{
    using System;
    using Common;

    internal class DaemonExecutorMetadata : TaskExecutorMetadata
    {
        private readonly Func<DaemonExecutor> _daemonExecutorConstructor;

        public DaemonExecutorMetadata(ILogger logger, Func<DaemonExecutor> daemonExecutorConstructor)
        {
            Logger = logger;
            _daemonExecutorConstructor = daemonExecutorConstructor;
            IsEnabled = true;
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

        public override TimeSpan? GetExecutionTimeout()
        {
            return null;
        }

        protected override TaskExecutor ExecutorActivator()
        {
            return _daemonExecutorConstructor();
        }
    }
}