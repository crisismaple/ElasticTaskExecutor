namespace ElasticTaskExecutor.Core
{
    using System;
    using System.Threading.Tasks;

    public interface ITaskExecutor : IDisposable
    {
        Task RunTaskAsync();
    }
}