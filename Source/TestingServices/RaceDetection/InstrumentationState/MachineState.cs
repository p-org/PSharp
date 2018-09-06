// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.RaceDetection.Util;
using static System.Diagnostics.Debug;

namespace Microsoft.PSharp.TestingServices.RaceDetection.InstrumentationState
{
    internal class MachineState
    {
        private long Mid;

        private ILogger Logger;

        public VectorClock VC { get; private set; }

        public long Epoch { get; private set; }

        private bool EnableLogging;

        public MachineState(MachineState other)
        {
            this.Mid = other.Mid;
            VC = new VectorClock(other.VC);
            this.Epoch = other.Epoch;
            Assert(VC.GetComponent(Mid) == Epoch, "Epoch and VC inconsistent!");
            this.EnableLogging = other.EnableLogging;
            this.Logger = other.Logger;
        }

        public MachineState(ulong id, ILogger logger, bool enableLogging)
        {
            this.VC = new VectorClock(0);
            this.Mid = (long)id;
            this.Logger = logger;
            this.EnableLogging = enableLogging;
            this.IncrementEpochAndVC();  // initialize
            Assert(VC.GetComponent(Mid) == Epoch, "Epoch and VC inconsistent!");
            if (EnableLogging)
            {
                Logger.WriteLine($"<VCLog> Created {VC.ToString()} for {Mid}");
            }
        }

        public void IncrementEpochAndVC()
        {
            Epoch = VC.Tick(Mid);
            Assert(VC.GetComponent(Mid) == Epoch, "Epoch and VC inconsistent!");
            if (EnableLogging)
            {
                Logger.WriteLine($"<VCLog> Updated {VC.ToString()} for {Mid} [Increment]");
            }
        }

        public void JoinEpochAndVC(VectorClock other)
        {
            VC.Max(other);
            Epoch = this.VC.GetComponent(this.Mid);
            Assert(VC.GetComponent(Mid) == Epoch, "Epoch and VC inconsistent!");
            if (EnableLogging)
            {
                Logger.WriteLine($"<VCLog> Updated {VC.ToString()} for {Mid} [Join {other.ToString()}]");
            }
        }

        public void JoinThenIncrement(VectorClock other)
        {
            JoinEpochAndVC(other);
            IncrementEpochAndVC();
            Assert(VC.GetComponent(Mid) == Epoch, "Epoch and VC inconsistent!");
            if (EnableLogging)
            {
                Logger.WriteLine($"<VCLog> Updated {VC.ToString()} for {Mid}");
            }
        }

        public int GetVCSize()
        {
            return VC.Size();
        }
    }
}