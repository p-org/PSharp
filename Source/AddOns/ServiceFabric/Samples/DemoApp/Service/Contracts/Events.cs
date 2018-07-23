namespace PoolServicesContract
{
    using Microsoft.PSharp;
    using System.Runtime.Serialization;

    [DataContract]
    public class ePoolDriverConfigChange : Event
    {
        [DataMember]
        public PoolDriverConfig Configuration;
    }

    [DataContract]
    public class ePoolDeletionRequest : Event
    {
    }

    [DataContract]
    public class ePoolResizeRequest : Event
    {
        [DataMember]
        public int Size;
    }
}
