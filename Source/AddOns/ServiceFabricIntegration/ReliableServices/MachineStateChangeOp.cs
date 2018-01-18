using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    /// <summary>
    /// Class representing an opertion for changing
    /// the state of a machine
    /// </summary>
    internal abstract class MachineStateChangeOp
    {
    }

    /// <summary>
    /// PushState operation
    /// </summary>
    internal class PushStateChangeOp : MachineStateChangeOp
    {
        public MachineState state;

        public PushStateChangeOp(MachineState state)
        {
            this.state = state;
        }
    }

    /// <summary>
    /// PopState operation
    /// </summary>
    internal class PopStateChangeOp : MachineStateChangeOp
    {
        public PopStateChangeOp()
        {

        }
    }

}


