using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices.Timers
{
    class ReliableTimerMock : ISingleTimer
    {
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
        /// ID of the machine requesting the timeout
        /// </summary>
        MachineId RSMId;

        /// <summary>
        /// The timer machine
        /// </summary>
        MachineId TimerId;

        /// Initializes a timer
        /// <param name="period">Period (ms)</param>
        /// <param name="name">Name</param>
        public ReliableTimerMock(MachineId RSMId, int period, string name)
        {
            this.RSMId = RSMId;
            this._Name = name;
            this._Period = period;
        }

        /// <summary>
        /// Starts the timer
        /// </summary>
        public void StartTimer()
        {
            TimerId = RSMId.Runtime.CreateMachine(typeof(MockTimerMachine), 
                new MockTimerMachine.InitTimer(RSMId, Name));
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        /// <returns>true if the timer was successfully cancelled, false otherwise</returns>
        public bool StopTimer()
        {
            RSMId.Runtime.SendEvent(TimerId, new MockTimerMachine.CancelTimer());
            return false;
        }

    }
}
