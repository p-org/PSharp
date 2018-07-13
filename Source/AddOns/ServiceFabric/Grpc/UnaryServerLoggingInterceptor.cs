namespace Microsoft.ServiceFabric.Grpc
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using global::Grpc.Core;
    using global::Grpc.Core.Interceptors;
    using Microsoft.Extensions.Logging;

    public class UnaryServerLoggingInterceptor : Interceptor
    {
        public UnaryServerLoggingInterceptor(ILogger logger)
        {
            this.Log = logger;
        }

        public ILogger Log { get; }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            DateTime startTime = DateTime.UtcNow;
            StringBuilder builder = new StringBuilder();
            foreach (var item in context.RequestHeaders)
            {
                builder.Append($"{item.Key}={item.Value};");
            }

            // Cheap tracking
            Guid id = Guid.NewGuid();
            Log.LogTrace("Invoking {0} with ID = {1} and with headers {2}", context.Method, id, builder.ToString());
            var data = await base.UnaryServerHandler(request, context, continuation);
            Log.LogTrace("Completed {0} with ID = {1} in {2} ms", context.Method, id, DateTime.UtcNow.Subtract(startTime).TotalMilliseconds);
            return data;
        }
    }
}
