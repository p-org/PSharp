using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using System.Runtime.Serialization;

namespace DemoAppConsole
{
    [DataContract]
    public class eInitClient : Event
    {
        [DataMember]
        public MachineId driver;

        public eInitClient(MachineId driver)
        {
            this.driver = driver;
        }
    }
}
