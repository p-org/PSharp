﻿namespace PoolDriver
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
            ConsoleLogger logger = new ConsoleLogger("PoolDriver", null, false);
            try
            {
                ServiceRuntime.RegisterServiceAsync("PoolDriverType",
                        context =>
                        {
                            return new PoolDriverService(context, logger);
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
