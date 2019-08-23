using System;

namespace Microsoft.PSharp.Timers
{
    /// <summary>
    /// Interface of a timer that can send timeout events to its owner machine.
    /// </summary>
    internal interface IMachineTimer : IDisposable
    {
        /// <summary>
        /// Stores information about this timer.
        /// </summary>
        TimerInfo Info { get; }
    }
}
