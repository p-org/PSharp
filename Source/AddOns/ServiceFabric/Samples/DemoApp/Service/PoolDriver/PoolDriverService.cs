namespace PoolDriver
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;

    public class PoolDriverService : PSharpService
    {
        public PoolDriverService(StatefulServiceContext serviceContext, ILogger logger) : base(serviceContext, new List<Type>())
        {
            this.Logger = logger;
        }

        public PoolDriverService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger) : base(serviceContext,  new List<Type>(), reliableStateManagerReplica)
        {
            this.Logger = logger;
        }

        public ILogger Logger { get; }
        
        public override async Task<List<ResourceTypesResponse>> ListResourceTypesAsync()
        {
            // Ideally this should also be on PSharp service - the runtime should go over known types
            // and figure out the reliable machines that it can host
            var data = await base.ListResourceTypesAsync();
            data.Add(new ResourceTypesResponse()
            {
                ResourceType = "PoolDriverMachine",
                Count = 1,
                MaxCapacity = 1
            });

            return data;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);
            await Task.Delay(-1, cancellationToken);
        }

        protected override IPSharpEventSourceLogger GetPSharpRuntimeLogger()
        {
            return new MyLogger();
        }

        private class MyLogger : IPSharpEventSourceLogger
        {
            public void Message(string message)
            {
                ServiceEventSource.Current.Message(message);
            }

            public void Message(string message, params object[] args)
            {
                ServiceEventSource.Current.Message(message, args);
            }
        }
    }
}
