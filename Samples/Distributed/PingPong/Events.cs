using System.Runtime.Serialization;
using Microsoft.PSharp;

namespace PingPong
{
    #region Events

    [DataContract]
    internal class Config : Event
    {
        [DataMember]
        public MachineId Id;

        public Config(MachineId id)
            : base(-1, -1)
        {
            this.Id = id;
        }
    }

    [DataContract]
    internal class Unit : Event { }

    [DataContract]
    internal class Ping : Event { }

    [DataContract]
    internal class Pong : Event { }

    #endregion
}
