using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timers
{
    class InitTimer : Event
    {
		public MachineId client;

		public InitTimer(MachineId client)
		{
			this.client = client;
		}
	}
}
