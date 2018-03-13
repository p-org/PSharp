using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timers
{

	#region internal events
	class InitTimer : Event
	{
		/// <summary>
		/// Id of the machine creating the timer.
		/// </summary>
		public MachineId client;

		/// <summary>
		/// True if periodic timeout events are desired.
		/// </summary>
		public bool IsPeriodic;

		public int period;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="client">Id of the machine creating the timer.</param>
		/// <param name="IsPeriodic">True if periodic timeout events are desired.</param>
		public InitTimer(MachineId client, bool IsPeriodic)
		{
			this.client = client;
			this.IsPeriodic = IsPeriodic;
		}

		public InitTimer(MachineId client, bool IsPeriodic, int period)
		{
			this.client = client;
			this.IsPeriodic = IsPeriodic;
			this.period = period;
		}
	}

	/// <summary>
	/// Event used to flush the queue of a machine of eTimeout events.
	/// A single Markup event is dispatched to the queue. Then all eTimeout events are removed until we see the Markup event.
	/// </summary>
	class Markup : Event { }

	class Unit : Event { }
	#endregion

	#region public events
	/// <summary>
	/// Timeout event sent by the timer.
	/// </summary>
	public class eTimeout : Event
	{
		/// <summary>
		/// Id of the machine generating the eTimeout event.
		/// </summary>
		public MachineId id;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="id">Id of the machine generating the eTimeout event.</param>
		public eTimeout(MachineId id)
		{
			this.id = id;
		}
	}

	/// <summary>
	/// Event requesting stoppage of timer.
	/// </summary>
	public class HaltTimer : Event
	{
		/// <summary>
		/// Id of machine invoking the request to stop the timer.
		/// </summary>
		public MachineId client;

		/// <summary>
		/// True if the user wants to flush the client's inbox of the relevant timeout messages.
		/// </summary>
		public bool flush;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="client">Id of machine invoking the request to stop the timer. </param>
		/// <param name="flush">True if the user wants to flush the inbox of relevant timeout messages.</param>
		public HaltTimer(MachineId client, bool flush)
		{
			this.client = client;
			this.flush = flush;
		}
	}
	#endregion
}
