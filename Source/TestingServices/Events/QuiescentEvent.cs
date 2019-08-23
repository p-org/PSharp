// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Signals that a machine has reached quiescence.
    /// </summary>
    [DataContract]
    internal sealed class QuiescentEvent : Event
    {
        /// <summary>
        /// The id of the machine that has reached quiescence.
        /// </summary>
        public MachineId MachineId;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuiescentEvent"/> class.
        /// </summary>
        /// <param name="mid">The id of the machine that has reached quiescence.</param>
        public QuiescentEvent(MachineId mid)
        {
            this.MachineId = mid;
        }
    }
}
