using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PoolServicesContract
{
    [DataContract]
    public class PoolDriverConfig
    {
        [DataMember]
        public Dictionary<string, int> PoolData;
    }
}
