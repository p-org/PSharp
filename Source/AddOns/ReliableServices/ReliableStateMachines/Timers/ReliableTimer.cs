using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices.Timers
{
    /// <summary>
    /// A Timer for Reliable State Machines
    /// </summary>
    class ReliableTimerProd : ISingleTimer
    {
        /// <summary>
        /// For cancelling the timer
        /// </summary>
        System.Threading.CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// Timeout duration
        /// </summary>
        readonly int _Period;

        public int TimePeriod
        {
            get
            {
                return _Period;
            }
        }

        /// <summary>
        ///  Name of the timer
        /// </summary>
        readonly string _Name;

        public string Name
        {
            get
            {
                return _Name;
            }
        }

        /// <summary>
        /// True if the timer has been cancelled
        /// </summary>
        bool TimerCancelled;

        /// <summary>
        /// True if the timer has fired (or is guaranteed to fire)
        /// </summary>
        bool TimeoutFired;

        /// <summary>
        /// Shared queue of timeouts
        /// </summary>
        LinkedList<string> TimeoutQueue;

        /// Initializes a timer
        /// <param name="TimeoutQueue">Timeout queue</param>
        /// <param name="period">Period (ms)</param>
        /// <param name="name">Name</param>
        public ReliableTimerProd(LinkedList<string> TimeoutQueue, int period, string name)
        {
            this.TimeoutQueue = TimeoutQueue;
            this._Period = period;
            this._Name = name;
            this.TimerCancelled = false;
            this.TimeoutFired = false;
            CancellationTokenSource = new System.Threading.CancellationTokenSource();
        }

        /// <summary>
        /// Starts the timer
        /// </summary>
        public void StartTimer()
        {
            Task.Run(async () =>
            {
                await Task.Delay(TimePeriod, CancellationTokenSource.Token);

                lock (TimeoutQueue)
                {
                    if(!TimerCancelled)
                    {
                        TimeoutQueue.AddFirst(Name);
                        TimeoutFired = true;
                    }
                }
            });
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        /// <returns>true if the timer was successfully cancelled, false otherwise</returns>
        public bool StopTimer()
        {
            lock(TimeoutQueue)
            {
                if(TimeoutFired)
                {
                    TimeoutQueue.Remove(Name);
                }
                TimerCancelled = true;
            }

            CancellationTokenSource.Cancel();

            return true;
        }
    }
}
