using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Microsoft.PSharp.ServiceFabric
{
    class EventMessageFactory : IServiceRemotingMessageBodyFactory
    {
        public IServiceRemotingRequestMessageBody CreateRequest(string interfaceName, string methodName,
            int numberOfParameters)
        {
            return new EventRemotingRequestBody();
        }

        public IServiceRemotingResponseMessageBody CreateResponse(string interfaceName, string methodName)
        {
            return new EventRemotingResponseBody();
        }
    }
}
