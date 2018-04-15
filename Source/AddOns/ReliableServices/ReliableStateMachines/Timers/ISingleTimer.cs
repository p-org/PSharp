using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices.Timers
{
    interface ISingleTimer
    {
        /// <summary>
        /// Name of the timer
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Time period (ms)
        /// </summary>
        int TimePeriod { get; }

        /// <summary>
        /// Starts the timer
        /// </summary>
        void StartTimer();

        /// <summary>
        /// Stops the timer
        /// </summary>
        /// <returns>True if timer was successfully cancelled</returns>
        bool StopTimer();
    }
}
