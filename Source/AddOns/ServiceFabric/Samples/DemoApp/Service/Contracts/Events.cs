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
}
