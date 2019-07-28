namespace ElasticTaskExecutor.UnitTest.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Core.Common;

    public class DummyLogger :ILogger
    {
        private ConcurrentBag<string> _infoMessages = new ConcurrentBag<string>();
        private ConcurrentBag<string> _warnMessages = new ConcurrentBag<string>();
        private ConcurrentBag<string> _errorMessages = new ConcurrentBag<string>();
        private ConcurrentBag<string> _fatalMessages = new ConcurrentBag<string>();
        private ConcurrentBag<string> _debugMessages = new ConcurrentBag<string>();


        public void LogInfo(string info)
        {
            Console.WriteLine(info);
        }

        public void LogWarning(string warning)
        {
            _warnMessages.Add(warning);
        }

        public void LogError(string error)
        {
            _errorMessages.Add(error);
        }

        public void LogFatal(string fatalInfo)
        {
            _fatalMessages.Add(fatalInfo);
        }

        public void LogDebug(string fatalInfo)
        {
            _debugMessages.Add(fatalInfo);
        }
    }
}