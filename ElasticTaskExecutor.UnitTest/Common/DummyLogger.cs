namespace ElasticTaskExecutor.UnitTest.Common
{
    using System.Collections.Generic;
    using Core.Common;

    public class DummyLogger :ILogger
    {
        private List<string> _infoMessages = new List<string>();
        private List<string> _warnMessages = new List<string>();
        private List<string> _errorMessages = new List<string>();
        private List<string> _fatalMessages = new List<string>();
        private List<string> _debugMessages = new List<string>();


        public void LogInfo(string info)
        {
            _infoMessages.Add(info);
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