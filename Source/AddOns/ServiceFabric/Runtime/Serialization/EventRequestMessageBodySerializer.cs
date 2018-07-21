using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.ServiceFabric
{
    class EventRequestMessageBodySerializer : IServiceRemotingRequestMessageBodySerializer
    {
        List<Type> knownTypes;

        public EventRequestMessageBodySerializer(Type serviceInterfaceType,
            IEnumerable<Type> parameterInfo, IEnumerable<Type> knownTypes)
        {
            this.knownTypes = new List<Type>(knownTypes);
            this.knownTypes.Add(typeof(MachineId));
        }

        public OutgoingMessageBody Serialize(IServiceRemotingRequestMessageBody serviceRemotingRequestMessageBody)
        {
            if (serviceRemotingRequestMessageBody == null)
            {
                return null;
            }

            var ser = new DataContractSerializer(typeof(EventRemotingRequestBody), knownTypes);
            var memoryStream = new MemoryStream();
            ser.WriteObject(memoryStream, serviceRemotingRequestMessageBody);
            memoryStream.Flush();
            var bytes = memoryStream.ToArray();

            var segment = new ArraySegment<byte>(bytes);
            var list = new List<ArraySegment<byte>> { segment };
            return new OutgoingMessageBody(list);
        }

        public IServiceRemotingRequestMessageBody Deserialize(IncomingMessageBody messageBody)
        {
            var buf = messageBody.GetReceivedBuffer();
            if (buf.Length == 0)
            {
                return null;
            }
            var ser = new DataContractSerializer(typeof(EventRemotingRequestBody), knownTypes);
            return (EventRemotingRequestBody)ser.ReadObject(messageBody.GetReceivedBuffer());
        }
    }
}
