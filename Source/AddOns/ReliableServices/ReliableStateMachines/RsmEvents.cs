using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    public class RsmInitEvent : Event
    {
        public readonly RsmHost Host;

        public RsmInitEvent(RsmHost host)
        {
            this.Host = host;
        }
    }
}
