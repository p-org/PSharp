namespace ResourceManager.SF
{
    using System.Fabric;
    using System.Threading.Tasks;
    using Grpc.Core;
    using Microsoft.Extensions.Logging;
    using Microsoft.ServiceFabric.Data;
    using ResourceManager.Contracts;
    using static ResourceManager.Contracts.ResourceManagerService;

    public class RmRpc : ResourceManagerServiceBase
    {
        private StatefulServiceContext context;
        private ILogger logger;
        private IReliableStateManager stateManager;

        public RmRpc(StatefulServiceContext context, ILogger logger, IReliableStateManager stateManager)
        {
            this.context = context;
            this.logger = logger;
            this.stateManager = stateManager;
        }

        public override async Task<CreateResourceResponse> CreateResource(CreateResourceRequest request, ServerCallContext context)
        {
            return new CreateResourceResponse() { ResourceId = "SUPER!" };
        }

        public override async Task<DeleteResourceResponse> DeleteResource(DeleteResourceRequest request, ServerCallContext context)
        {
            return new DeleteResourceResponse() { Result = "SUPER!" };
        }

        public override Task ListResourceTypes(ListResourceTypesRequest request, IServerStreamWriter<ResourceTypesResponse> responseStream, ServerCallContext context)
        {
            return base.ListResourceTypes(request, responseStream, context);
        }
    }
}
