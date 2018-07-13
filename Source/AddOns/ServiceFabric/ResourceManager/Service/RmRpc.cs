using System.Fabric;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Data;
using ResourceManager.Contracts;

namespace ResourceManager.SF
{
    public class RmRpc : Contracts.ResourceManagerService.ResourceManagerServiceBase
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

        public override Task<CreateResourceResponse> CreateResource(CreateResourceRequest request, ServerCallContext context)
        {
            return base.CreateResource(request, context);
        }

        public override Task<DeleteResourceResponse> DeleteResource(DeleteResourceRequest request, ServerCallContext context)
        {
            return base.DeleteResource(request, context);
        }

        public override Task ListResourceTypes(ListResourceTypesRequest request, IServerStreamWriter<ResourceTypesResponse> responseStream, ServerCallContext context)
        {
            return base.ListResourceTypes(request, responseStream, context);
        }
    }
}
