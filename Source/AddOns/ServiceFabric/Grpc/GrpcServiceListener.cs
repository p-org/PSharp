namespace Microsoft.ServiceFabric.Grpc
{
    using global::Grpc.Core;
    using global::Grpc.Core.Interceptors;
    using Microsoft.Extensions.Logging;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;

    public class GrpcServiceListener : ICommunicationListener
    {
        // https://github.com/sceneskope/service-fabric-protocol-buffers/tree/37b405365c31501f17c79b8d14d2ac61d5189b6f/SceneSkope.ServiceFabric.GrpcRemoting
        //
        // Summary:
        //     Services that will be exported by the server once started. Register a service
        //     with this server by adding its definition to this collection.
        public IEnumerable<ServerServiceDefinition> Services { get; }

        public StatefulServiceContext Context { get; }

        public ILogger Log { get; }

        public string EndpointName { get; }

        public GrpcServiceListener(StatefulServiceContext context, ILogger logger, IEnumerable<ServerServiceDefinition> services,
            string endpointName = "GrpcServiceEndpoint")
        {
            Context = context;
            Log = logger;
            Services = services;
            EndpointName = endpointName;
        }

        public void Abort()
        {
            Log.LogDebug("Aborting server");
            StopServerAsync().Wait();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Log.LogDebug("Closing server");
            return StopServerAsync();
        }

        private Server _server;

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var serviceEndpoint = Context.CodePackageActivationContext.GetEndpoint(EndpointName);
            var port = serviceEndpoint.Port;
            var host = FabricRuntime.GetNodeContext().IPAddressOrFQDN;

            Log.LogDebug("Starting gRPC server on http://{Host}:{Port}", host, port);
            try
            {
                var server = new Server
                {
                    Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
                };

                foreach (ServerServiceDefinition service in Services)
                {
                    // Intercept calls for each service
                    service.Intercept(new UnaryServerLoggingInterceptor(Log));
                    server.Services.Add(service);
                }

                _server = server;
                server.Start();
                return $"http://{host}:{port}";
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Error starting server: {Exception}", ex.Message);
                await StopServerAsync().ConfigureAwait(false);
                throw;
            }
        }

        private Task StopServerAsync()
        {
            Log.LogDebug("Stopping gRPC server");
            return InternalStopServerAsync();
        }

        private async Task InternalStopServerAsync()
        {
            Log.LogDebug("Really stopping server - or at least trying");
            try
            {
                await _server?.KillAsync();
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Failed to shutdown server: {Exception}", ex.Message);
            }
            Log.LogDebug("Probably shutdown");
        }
    }
}
