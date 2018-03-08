//-----------------------------------------------------------------------
// <copyright file="Timers.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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
		/// Flag to prevent timeout events being sent after stopping the timer.
		/// </summary>
		private volatile bool IsTimerEnabled = false;

		/// <summary>
		/// Used to synchronize the Elapsed event handler with timer stoppage.
		/// </summary>
		private readonly Object tlock = new object();

		#endregion

		#region Timer API

		/// <summary>
		/// Start a timer. 
		/// </summary>
		protected void StartTimer()
		{
			// For production code, use the system timer.
			if (!IsTestingMode)
			{
				this.timer = new System.Timers.Timer(period);

				if (!IsPeriodic)
				{
					this.timer.AutoReset = false;
				}

				this.timer.Elapsed += ElapsedEventHandler;
				this.IsTimerEnabled = true;
				this.timer.Start();
			}

			// In testing mode, create a timer model, and pass the identifier of the calling machine.
			else
			{
				this.modelTimer = this.CreateMachine(typeof(TimerModel), new InitTimer(this.Id));
			}
		}

		/// <summary>
		/// Stop the timer.
		/// </summary>
		/// <param name="flush">True if the user wants to flush the input queue of all eTimeout events.</param>
		protected void StopTimer(bool flush)
		{
			if (!IsPeriodic)
			{
				lock (tlock)
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

			// Clear the client machine's queue of eTimeout events.
			if (flush)
			{
				this.Send(this.Id, new Markup());

				while (this.Receive(typeof(Markup), typeof(eTimeout)).GetType() != (typeof(Markup))) { }
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
			lock (this.tlock)
			{
				if (this.IsTimerEnabled)
				{
					Runtime.SendEvent(this.Id, new eTimeout());
				}
			}
		}

		#endregion
	}
}