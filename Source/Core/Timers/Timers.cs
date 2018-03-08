using System;
using System.Timers;

namespace Microsoft.PSharp.Timers
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class Timer : Machine
	{
		#region public fields
		/// <summary>
		/// False if running in production mode.
		/// </summary>
		public static bool testMode;

		#endregion

		#region private fields

		/// <summary>
		/// System timer to generate Elapsed timeout events in production mode.
		/// </summary>
		private System.Timers.Timer timer;

		/// <summary>
		/// Flag to prevent timeout events being sent after stopping the timer.
		/// </summary>
		private volatile bool isTimerEnabled = false;

		/// <summary>
		/// Used to synchronize Elapsed event handler with timer stoppage.
		/// </summary>
		private readonly Object tlock = new object();

		#endregion

		#region Timer API

		/// <summary>
		/// Start a timer. 
		/// If isPeriodic is set, then timeout events are sent periodically (default period 100ms).
		/// </summary>
		/// <param name="isPeriodic">True if a periodic timer is desired.</param>
		protected void Start(bool isPeriodic)
		{
			// For production code, use the system timer.
			if (!testMode)
			{
				if(isPeriodic)
				{
					this.timer = new System.Timers.Timer();
					this.timer.Elapsed += OnTimedEvent;
					this.isTimerEnabled = true;
					this.timer.Start();
				}
			}
		}

		/// <summary>
		/// Start a timer.
		/// If isPeriodic is set, then timeout events are sent periodically, with 'period' periodicity.
		/// </summary>
		/// <param name="isPeriodic">True if a periodic timer is desired.</param>
		/// <param name="period">Periodicity of the timer.</param>
		protected void Start(bool isPeriodic, int period)
		{

		}

		#endregion

		#region private methods

		private void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			lock(this.tlock)
			{
				if(this.isTimerEnabled)
				{
					Runtime.SendEvent(this.Id, new eTimeout());
				}
			}
		}

		#endregion
	}
}