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
            this.VC = new VectorClock(other.VC);
            this.Epoch = other.Epoch;
            Assert(this.VC.GetComponent(this.Mid) == this.Epoch, "Epoch and VC inconsistent!");
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
            Assert(this.VC.GetComponent(this.Mid) == this.Epoch, "Epoch and VC inconsistent!");
            if (this.EnableLogging)
            {
                this.Logger.WriteLine($"<VCLog> Created {this.VC.ToString()} for {this.Mid}");
            }
        }

        public void IncrementEpochAndVC()
        {
            this.Epoch = this.VC.Tick(this.Mid);
            Assert(this.VC.GetComponent(this.Mid) == this.Epoch, "Epoch and VC inconsistent!");
            if (this.EnableLogging)
            {
                this.Logger.WriteLine($"<VCLog> Updated {this.VC.ToString()} for {this.Mid} [Increment]");
            }
        }

        public void JoinEpochAndVC(VectorClock other)
        {
            this.VC.Max(other);
            this.Epoch = this.VC.GetComponent(this.Mid);
            Assert(this.VC.GetComponent(this.Mid) == this.Epoch, "Epoch and VC inconsistent!");
            if (this.EnableLogging)
            {
                this.Logger.WriteLine($"<VCLog> Updated {this.VC.ToString()} for {this.Mid} [Join {other.ToString()}]");
            }
        }

        public void JoinThenIncrement(VectorClock other)
        {
            this.JoinEpochAndVC(other);
            this.IncrementEpochAndVC();
            Assert(this.VC.GetComponent(this.Mid) == this.Epoch, "Epoch and VC inconsistent!");
            if (this.EnableLogging)
            {
                this.Logger.WriteLine($"<VCLog> Updated {this.VC.ToString()} for {this.Mid}");
            }
        }

        public int GetVCSize()
        {
            return this.VC.Size();
        }
    }
}
