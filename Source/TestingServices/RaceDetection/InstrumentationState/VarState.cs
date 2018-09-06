// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.TestingServices.RaceDetection.Util;
using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.RaceDetection.InstrumentationState
{
    internal class VarState
    {
        internal long ReadEpoch;

        internal long WriteEpoch;

        internal VectorClock VC;

        internal string lastWriteLocation;

        internal Dictionary<long, string> LastReadLocation;

        internal long InMonitorWrite;

        internal Dictionary<long, long> InMonitorRead;

        public VarState(bool isWrite, long epoch, bool shouldCreateInstrumentationState, long inMonitor)
        {
            if (isWrite)
            {
                ReadEpoch = Epoch.Zero;
                WriteEpoch = epoch;
                InMonitorWrite = inMonitor;
            }
            else
            {
                WriteEpoch = Epoch.Zero;
                ReadEpoch = epoch;
            }

            if (shouldCreateInstrumentationState)
            {
                LastReadLocation = new Dictionary<long, string>();
            }
            InMonitorRead = new Dictionary<long, long>();
        }
    }
}