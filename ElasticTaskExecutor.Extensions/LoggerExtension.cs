using Microsoft.Extensions.Logging;

namespace ElasticTaskExecutor.Extensions
{
    using System;

    public static class LoggerExtension
    {
        private const string ExceptionLogTemplate = @"Met exception in {0}: {1}";

        public static void LogExceptionInError(this ILogger logger, string methodName, Exception e)
        {
            logger?.LogError(e, string.Format(ExceptionLogTemplate, methodName, e));
        }

        public static void LogExceptionInWarn(this ILogger logger, string methodName, Exception e)
        {
            logger?.LogWarning(e, string.Format(ExceptionLogTemplate, methodName, e));
        }
    }
}