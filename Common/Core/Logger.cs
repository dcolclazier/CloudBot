using DiscordBot.Common.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace DiscordBot.Common.Core
{
	public sealed class Logger
    {
        public static LoggerFactory Factory { get; }
        private Logger() { }
        static Logger()
        {
            Factory = new LoggerFactory();
            Configure(Factory);
        }

        public static void Configure(ILoggerFactory factory)
        {
#if DEBUG
            var currentLevel = LogLevel.Trace;
#else
            var currentLevel = LogLevel.Error;
#endif
            if (Environment.GetEnvironmentVariable("LOGGING_LEVEL").IsInt(out var logLevel))
            {
                currentLevel = (LogLevel)logLevel;
            }
            factory.AddLambdaLogger(new LambdaLoggerOptions
            {
                Filter = (s, level) => level >= currentLevel
            });
        }

        public static ILogger GetLogger(string category) => Factory.CreateLogger($"{category} - {Assembly.GetExecutingAssembly().GetName().Version}");

	}


}
