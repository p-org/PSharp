// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.RaceDetection.Util;

namespace Microsoft.PSharp.TestingServices.RaceDetection.InstrumentationState
{
    internal class VarState
    {
        internal long ReadEpoch;

        internal long WriteEpoch;

        internal VectorClock VC;

        internal string LastWriteLocation;

        internal Dictionary<long, string> LastReadLocation;

        internal long InMonitorWrite;

        internal Dictionary<long, long> InMonitorRead;

        public VarState(bool isWrite, long epoch, bool shouldCreateInstrumentationState, long inMonitor)
        {
            if (isWrite)
            {
                this.ReadEpoch = Epoch.Zero;
                this.WriteEpoch = epoch;
                this.InMonitorWrite = inMonitor;
            }
            else
            {
                this.WriteEpoch = Epoch.Zero;
                this.ReadEpoch = epoch;
            }

            if (shouldCreateInstrumentationState)
            {
                this.LastReadLocation = new Dictionary<long, string>();
            }

            this.InMonitorRead = new Dictionary<long, long>();
        }
    }
}
