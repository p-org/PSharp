namespace PoolServicesContract
{
    using Microsoft.PSharp;
    using System.Runtime.Serialization;

    [DataContract]
    public class ePoolDriverConfigChangeEvent : Event
    {
        [DataMember]
        public PoolDriverConfig Configuration;
    }

    [DataContract]
    public class ePoolDeletionRequestEvent : Event
    {
    }

    [DataContract]
    public class ePoolResizeRequestEvent : Event
    {
        [DataMember]
        public int Size;
    }

    [DataContract]
    public class eVMCreateRequestEvent : Event
    {
        public eVMCreateRequestEvent(MachineId sId)
        {
            this.senderId = sId;
        }

        [DataMember]
        public MachineId senderId;
    }

    [DataContract]
    public class eVMRenewRequestEvent : Event
    {
        public eVMRenewRequestEvent(MachineId sId)
        {
            this.senderId = sId;
        }

        [DataMember]
        public MachineId senderId;
    }

    [DataContract]
    public class eVMDeleteRequestEvent : Event
    {
        public eVMDeleteRequestEvent(MachineId sId)
        {
            this.senderId = sId;
        }

        [DataMember]
        public MachineId senderId;
    }
}
