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
		public static bool IsTestingMode;

		/// <summary>
		/// False if the timer sends a single timeout event.
		/// True if the timer sends timeouts at regular intervals.
		/// </summary>
		public static bool IsPeriodic;

		/// <summary>
		/// If the timer is periodic, periodicity of the timeout events.
		/// Default is 100ms, same as the default in System.Timers.Timer
		/// </summary>
		public static int period = 100;

		#endregion

		#region private fields

		/// <summary>
		/// System timer to generate Elapsed timeout events in production mode.
		/// </summary>
		private System.Timers.Timer timer;

		/// <summary>
		/// Model timers generating timeout events in test mode.
		/// </summary>
		private MachineId modelTimer;

		/// <summary>
		/// Machine to which the timer model sends timeout events.
		/// </summary>
		private MachineId client;

		/// <summary>
		/// Flag to prevent timeout events being sent after stopping the timer.
		/// </summary>
		private volatile bool IsTimerEnabled = false;

		/// <summary>
		/// Used to synchronize the Elapsed event handler with timer stoppage.
		/// </summary>
		private readonly Object tlock = new object();

		#endregion

		#region internal events

		private class InitTimer : Event
		{
			public MachineId client;

			public InitTimer(MachineId client)
			{
				this.client = client;
			}
		}

		private class Unit : Event { }

		#endregion

		#region Timer API

		/// <summary>
		/// Start a timer. 
		/// </summary>
		protected void Start()
		{
			// For production code, use the system timer.
			if (!IsTestingMode)
			{
				this.timer = new System.Timers.Timer(period);

				if(!IsPeriodic)
				{
					this.timer.AutoReset = false;
				}
				
				this.timer.Elapsed += ElapsedEventHandler;
				this.IsTimerEnabled = true;
				this.timer.Start();
			}

			else
			{
				this.modelTimer = this.CreateMachine(typeof(Timer), new InitTimer(this.Id));
			}
		}

		/// <summary>
		/// Stop the timer.
		/// </summary>
		protected void Stop()
		{
			if(!IsPeriodic)
			{
				lock(tlock)
				{
					IsTimerEnabled = false;
					timer.Stop();
					timer.Dispose();
				}
			}

			else
			{
				this.Send(this.modelTimer, new Halt());
			}
		}

		private void ElapsedEventHandler(Object source, ElapsedEventArgs e)
		{
			lock (tlock)
			{
				if (IsTimerEnabled)
				{
					Runtime.SendEvent(this.Id, new eTimeout());
				}
			}
		}

		#endregion

		#region private methods

		private void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			lock(this.tlock)
			{
				if(this.IsTimerEnabled)
				{
					Runtime.SendEvent(this.Id, new eTimeout());
				}
			}
		}

		#endregion

		#region states

		[Start]
		[OnEntry(nameof(InitializeTimer))]
		private class Init : MachineState { }

		[OnEntry(nameof(SendTimeout))]
		[OnEventDoAction(typeof(Unit), nameof(SendTimeout))]
		private class Active : MachineState { }
		#endregion

		#region event handlers
		private void InitializeTimer()
		{
			this.client = (this.ReceivedEvent as InitTimer).client;
			this.Goto<Active>();
		}

		private void SendTimeout()
		{
			// If not periodic, send a single timeout event
			if(!IsPeriodic)
			{
				this.Send(this.client, new eTimeout());
			}
			else
			{
				if(this.Random())
				{
					this.Send(this.client, new eTimeout());
				}
				this.Raise(new Unit());
			}
		}
		#endregion
	}
}