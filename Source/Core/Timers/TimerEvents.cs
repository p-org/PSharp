//-----------------------------------------------------------------------
// <copyright file="TimerEvents.cs">
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

        /// <summary>
        /// Period
        /// </summary>
		public int Period;

        /// <summary>
        /// TimerId
        /// </summary>
        public TimerId tid;

        public InitTimer(MachineId client, TimerId tid, bool IsPeriodic, int period)
		{
			this.client = client;
			this.IsPeriodic = IsPeriodic;
			this.Period = period;
            this.tid = tid;
		}
	}

	/// <summary>
	/// Event used to flush the queue of a machine of eTimeout events.
	/// A single Markup event is dispatched to the queue. Then all eTimeout events are removed until we see the Markup event.
	/// </summary>
	class Markup : Event { }

    /// <summary>
    /// Signals next timeout period
    /// </summary>
	class RepeatTimeout : Event { }

    /// <summary>
    /// Event requesting stoppage of timer.
    /// </summary>
    class HaltTimerEvent : Event
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
        public HaltTimerEvent(MachineId client, bool flush)
        {
            this.client = client;
            this.flush = flush;
        }
    }
    #endregion

    #region public events
    /// <summary>
    /// Timeout event sent by the timer.
    /// </summary>
    public class TimerElapsedEvent : Event
	{
        /// <summary>
        /// TimerId
        /// </summary>
        public readonly TimerId Tid;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tid">Tid</param>
        public TimerElapsedEvent(TimerId tid)
		{
            this.Tid = tid;
		}
	}

	#endregion
}
