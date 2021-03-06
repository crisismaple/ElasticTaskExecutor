﻿namespace ElasticTaskExecutor.Core.Internal
{
    using System;
    using Common;

    internal class DaemonExecutorMetadata : TaskPullerMetadata
    {
        private readonly Func<DaemonExecutor> _daemonExecutorConstructor;

        public DaemonExecutorMetadata(Func<DaemonExecutor> daemonExecutorConstructor)
        {
            _daemonExecutorConstructor = daemonExecutorConstructor;
            TaskExecutorName = nameof(DaemonExecutor);
        }
        protected override int TaskPullerTypeId => Constraint.DaemonExecutorId;

        public override TimeSpan? ExecutionTimeout => null;

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