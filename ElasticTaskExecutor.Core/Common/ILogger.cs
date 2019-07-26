namespace ElasticTaskExecutor.Core.Common
{
    public interface ILogger
    {
        void LogInfo(string info);

        void LogWarning(string warning);

        void LogError(string error);

        void LogFatal(string fatalInfo);

        void LogDebug(string fatalInfo);
    }
}