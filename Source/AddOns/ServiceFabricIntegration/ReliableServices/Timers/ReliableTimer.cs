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
        /// Lock
        /// </summary>
        object lck;

        /// <summary>
        /// ID of the machine requesting the timeout
        /// </summary>
        MachineId RSMId;

        /// Initializes a timer
        /// <param name="period">Period (ms)</param>
        /// <param name="name">Name</param>
        public ReliableTimerProd(MachineId RSMId, int period, string name)
        {
            this.RSMId = RSMId;
            this._Period = period;
            this._Name = name;
            this.TimerCancelled = false;
            this.TimeoutFired = false;
            this.lck = new object();
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
                var cancelled = false;

                lock (lck)
                {
                    cancelled = TimerCancelled;
                    if (!cancelled)
                    {
                        TimeoutFired = true;
                    }
                }

                if (!cancelled)
                {
                    RSMId.Runtime.SendEvent(RSMId, new TimeoutEvent(Name));
                }
            });
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        /// <returns>true if the timer was successfully cancelled, false otherwise</returns>
        public bool StopTimer()
        {
            var status = true;

            lock(lck)
            {
                if(TimeoutFired)
                {
                    status = false;
                }
                TimerCancelled = true;
            }

            if (status)
            {
                CancellationTokenSource.Cancel();
            }

            return status;
        }
    }
}
