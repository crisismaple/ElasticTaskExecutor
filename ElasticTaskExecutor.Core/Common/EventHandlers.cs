namespace ElasticTaskExecutor.Core.Common
{
    using System;
    using Internal;

    public delegate void ExceptionEventHandler(object sender, Exception e);

    public delegate void ExecutorEventHandler(ExecutorBase executor);

}