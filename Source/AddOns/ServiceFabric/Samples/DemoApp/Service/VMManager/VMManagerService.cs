namespace VMManager
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;

    public class VMManagerService : PSharpService
    {
        public VMManagerService(StatefulServiceContext serviceContext, ILogger logger) : base(serviceContext, new List<Type>())
        {
            this.Logger = logger;
        }

        public VMManagerService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger) : base(serviceContext, new List<Type>(), reliableStateManagerReplica)
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
                ResourceType = "VMManagerMachine",
                Count = 1,
                MaxCapacity = 100
            });

            return data;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);
            await Task.Delay(-1, cancellationToken);
        }
    }
}
