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
		MachineId client;	// machine id of the client

		/// <summary>
		/// Constructor to initialize the field with the id of the client machine.
		/// </summary>
		/// <param name="client"></param>
		public InitTimer(MachineId client) : base()
		{
			this.client = client;
		}

		/// <summary>
		/// Returns the client machine id.
		/// </summary>
		/// <returns></returns>
		public MachineId getClientId()
		{
			return this.client;
		}
	}
}
