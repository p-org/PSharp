namespace VMManager
{
    using Microsoft.ServiceFabric.Services.Runtime;
    using Microsoft.Extensions.Logging.Console;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;

    internal class Program
    {
        public static void Main(string[] args)
        {
            ConsoleLogger logger = new ConsoleLogger("VMManager", null, false);
            try
            {
                ServiceRuntime.RegisterServiceAsync("VMManagerType",
                        context =>
                        {
                            return new VMManagerService(context, logger);
                        })
                    .GetAwaiter()
                    .GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(ex.ToString());
                logger.LogCritical(ex, "Exiting application");
                throw;
            }
        }
    }
}
