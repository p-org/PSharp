using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timer
{
	/// <summary>
	/// Event fired by a client machine to initialize a timer.
	/// </summary>
	public class InitTimer : Event
	{
		MachineId client;   // machine id of the client
		int period;			// periodicity of the timeout events

		/// <summary>
		/// Constructor to register the client, and set the periodicity of timeout events.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="period"></param>
		public InitTimer(MachineId client, int period) : base()
		{
			this.client = client;
			this.period = period;
		}

		/// <summary>
		/// Constructor to register the client, and set the periodicity to the default value of 100ms.
		/// </summary>
		/// <param name="client"></param>
		public InitTimer(MachineId client) : base()
		{
			this.client = client;
			this.period = 100;
		}

		/// <summary>
		/// Returns the client machine id.
		/// </summary>
		/// <returns></returns>
		public MachineId getClientId()
		{
			return this.client;
		}

		/// <summary>
		/// Get the intended periodicity.
		/// </summary>
		/// <returns></returns>
		public int getPeriod()
		{
			return this.period;
		}
	}
}
