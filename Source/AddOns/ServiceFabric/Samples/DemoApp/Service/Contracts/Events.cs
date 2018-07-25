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
    public class BaseEvent : Event
    {
        public BaseEvent(MachineId sId)
        {
            this.senderId = sId;
        }

        [DataMember]
        public MachineId senderId;
    }

    [DataContract]
    public class eVMCreateRequestEvent : BaseEvent
    {
        public eVMCreateRequestEvent(MachineId sId) : base(sId) { }
    }

    [DataContract]
    public class eVMRetryCreateRequestEvent : BaseEvent
    {
        public eVMRetryCreateRequestEvent(MachineId sId) : base(sId) { }
    }

    [DataContract]
    public class eVMRetryDeleteRequestEvent : BaseEvent
    {
        public eVMRetryDeleteRequestEvent(MachineId sId) : base(sId) { }
    }


    [DataContract]
    public class eVMDeleteRequestEvent : BaseEvent
    {
        public eVMDeleteRequestEvent(MachineId sId) : base(sId) { }
    }

    [DataContract]
    public class eVMCreateSuccessRequestEvent : BaseEvent
    {
        public eVMCreateSuccessRequestEvent(MachineId sId) : base(sId) { }
    }

    [DataContract]
    public class eVMCreateFailureRequestEvent : BaseEvent
    {
        public eVMCreateFailureRequestEvent(MachineId sId) : base(sId) { }
    }

    [DataContract]
    public class eVMDeleteSuccessRequestEvent : BaseEvent
    {
        public eVMDeleteSuccessRequestEvent(MachineId sId) : base(sId) { }
    }

    [DataContract]
    public class eVMDeleteFailureRequestEvent : BaseEvent
    {
        public eVMDeleteFailureRequestEvent(MachineId sId) : base(sId) { }
    }
}
