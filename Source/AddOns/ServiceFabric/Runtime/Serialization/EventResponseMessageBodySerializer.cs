using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.ServiceFabric
{
    class EventResponseMessageBodySerializer : IServiceRemotingResponseMessageBodySerializer
    {
        IEnumerable<Type> knownTypes;

        public EventResponseMessageBodySerializer(Type serviceInterfaceType,
            IEnumerable<Type> parameterInfo, IEnumerable<Type> knownEventTypes)
        {
            var kt = new List<Type>(knownEventTypes);
            kt.Add(typeof(MachineId));
            this.knownTypes = kt;
        }


        public OutgoingMessageBody Serialize(IServiceRemotingResponseMessageBody responseMessageBody)
        {
            var ser = new DataContractSerializer(typeof(EventRemotingResponseBody), knownTypes);
            var memoryStream = new MemoryStream();
            ser.WriteObject(memoryStream, responseMessageBody);
            memoryStream.Flush();

            var bytes = memoryStream.ToArray();
            var segment = new ArraySegment<byte>(bytes);
            var list = new List<ArraySegment<byte>> { segment };
            return new OutgoingMessageBody(list);
        }

        public IServiceRemotingResponseMessageBody Deserialize(IncomingMessageBody messageBody)
        {
            var ser = new DataContractSerializer(typeof(EventRemotingResponseBody), knownTypes);
            return (EventRemotingResponseBody)ser.ReadObject(messageBody.GetReceivedBuffer());
        }
    }
}
