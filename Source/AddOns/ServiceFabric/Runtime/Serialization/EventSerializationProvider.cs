using Microsoft.ServiceFabric.Services.Remoting.V2;
using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.ServiceFabric
{
    public class EventSerializationProvider : IServiceRemotingMessageSerializationProvider
    {
        IEnumerable<Type> knownTypes;

        public EventSerializationProvider(IEnumerable<Type> knownTypes)
        {
            this.knownTypes = knownTypes;
        }

        public IServiceRemotingMessageBodyFactory CreateMessageBodyFactory()
        {
            return new EventMessageFactory();
        }

        public IServiceRemotingRequestMessageBodySerializer CreateRequestMessageSerializer(Type serviceInterfaceType, IEnumerable<Type> requestBodyTypes)
        {
            return new EventRequestMessageBodySerializer(serviceInterfaceType, requestBodyTypes, knownTypes);
        }

        public IServiceRemotingResponseMessageBodySerializer CreateResponseMessageSerializer(Type serviceInterfaceType, IEnumerable<Type> responseBodyTypes)
        {
            return new EventResponseMessageBodySerializer(serviceInterfaceType, responseBodyTypes, knownTypes);
        }
    }
}
