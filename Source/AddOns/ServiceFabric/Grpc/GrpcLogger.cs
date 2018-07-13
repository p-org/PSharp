namespace Microsoft.ServiceFabric.Grpc
{
    using System;
    using Microsoft.Extensions.Logging;

    public class GrpcLogger : global::Grpc.Core.Logging.ILogger
    {
        public ILoggerFactory LoggerFactory { get; }
        public ILogger Log { get; }

        public GrpcLogger(ILoggerFactory loggerFactory) : this(loggerFactory, loggerFactory.CreateLogger<GrpcLogger>())
        {
        }

        public GrpcLogger(ILoggerFactory loggerFactory, ILogger logger)
        {
            LoggerFactory = loggerFactory;
            Log = logger;
        }

#pragma warning disable Serilog004 // Constant MessageTemplate verifier
        public void Debug(string message) => Log.LogDebug(message);

        public void Debug(string format, params object[] formatArgs) => Log.LogDebug(format, formatArgs);

        public void Error(string message) => Log.LogError(message);

        public void Error(string format, params object[] formatArgs) => Log.LogError(format, formatArgs);

        public void Error(Exception exception, string message) => Log.LogError(exception, message);

        public void Info(string message) => Log.LogInformation(message);

        public void Info(string format, params object[] formatArgs) => Log.LogInformation(format, formatArgs);

        public void Warning(string message) => Log.LogWarning(message);

        public void Warning(string format, params object[] formatArgs) => Log.LogInformation(format, formatArgs);

        public void Warning(Exception exception, string message) => Log.LogWarning(exception, message);

        global::Grpc.Core.Logging.ILogger global::Grpc.Core.Logging.ILogger.ForType<T>()
        {
            return new GrpcLogger(LoggerFactory, LoggerFactory.CreateLogger<T>());
        }
#pragma warning restore Serilog004 // Constant MessageTemplate verifier

    }
}
