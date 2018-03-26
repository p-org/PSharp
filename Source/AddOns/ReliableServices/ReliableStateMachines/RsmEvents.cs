using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    public class RsmInitEvent : Event
    {
        public readonly IRsmHost Host;

        public RsmInitEvent(IRsmHost host)
        {
            this.Host = host;
        }
    }
}
