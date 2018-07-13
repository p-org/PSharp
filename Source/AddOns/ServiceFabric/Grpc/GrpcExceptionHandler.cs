namespace Microsoft.ServiceFabric.Grpc
{
    using global::Grpc.Core;
    using Microsoft.Extensions.Logging;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    public class GrpcExceptionHandler : IExceptionHandler
    {
        public ILogger Log { get; }

        public GrpcExceptionHandler(ILogger logger)
        {
            Log = logger;
        }

        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            if (exceptionInformation.Exception is RpcException rpcEx)
            {
                switch (rpcEx.Status.StatusCode)
                {
                    case StatusCode.Unavailable when rpcEx.Status.Detail == "Endpoint read failed":
                        Log.LogDebug(exceptionInformation.Exception, "Throwing: {Exception}", exceptionInformation.Exception.Message);
                        result = null;
                        return false;

                    case StatusCode.Unavailable:
                    case StatusCode.Unknown:
                    case StatusCode.Cancelled:
                        Log.LogDebug(exceptionInformation.Exception, "Not transient exception: {Exception}, Retry {@Retry}", exceptionInformation.Exception.Message, retrySettings);
                        result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, int.MaxValue);
                        return true;

                    default:
                        Log.LogDebug(exceptionInformation.Exception, "Transient exception: {Exception}, Retry {@Retry}", exceptionInformation.Exception.Message, retrySettings);
                        result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, true, retrySettings, int.MaxValue);
                        return true;
                }
            }
            else
            {
                Log.LogDebug(exceptionInformation.Exception, "Throwing: {Exception}", exceptionInformation.Exception.Message);
                result = null;
                return false;
            }
        }
    }
}
