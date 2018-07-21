using Microsoft.ServiceFabric.Services.Remoting.V2;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.ServiceFabric
{
    [DataContract]
    class EventRemotingRequestBody : IServiceRemotingRequestMessageBody
    {
        [DataMember]
        public readonly Dictionary<string, object> parameters = new Dictionary<string, object>();

        public void SetParameter(int position, string parameName, object parameter)
        {
            this.parameters[parameName] = parameter;
        }

        public object GetParameter(int position, string parameName, Type paramType)
        {
            return this.parameters[parameName];
        }
    }
}
