using Microsoft.ServiceFabric.Services.Remoting.V2;
using System;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.ServiceFabric
{
    [DataContract]
    class EventRemotingResponseBody : IServiceRemotingResponseMessageBody
    {
        [DataMember]
        public object Value;

        public void Set(object response)
        {
            this.Value = response;
        }

        public object Get(Type paramType)
        {
            return this.Value;
        }
    }
}
