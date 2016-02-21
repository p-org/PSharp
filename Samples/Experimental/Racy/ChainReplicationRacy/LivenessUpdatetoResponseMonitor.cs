using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    class LivenessUpdatetoResponseMonitor : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public int reqId;

            public eInitialize(int reqId)
            {
                this.reqId = reqId;
            }
        }

        private class eLocal : Event { }

        public class eMonitorSuccess : Event { }

        public class eMonitorUpdateLiveness : Event
        {
            public int reqId;

            public eMonitorUpdateLiveness(int reqId)
            {
                this.reqId = reqId;
            }
        }

        public class eMonitorResponseLiveness : Event
        {
            public int reqId;

            public eMonitorResponseLiveness(int reqId)
            {
                this.reqId = reqId;
            }
        }
        #endregion

        #region fields
        private int MyRequestId;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(WaitForUpdateRequest))]
        private class WaitingForInit : MachineState { }

        [OnEventGotoState(typeof(eMonitorSuccess), typeof(WaitForResponse))]
        [OnEventDoAction(typeof(eMonitorUpdateLiveness), nameof(CheckIfMine))]
        [OnEventDoAction(typeof(eMonitorResponseLiveness), nameof(AssertNotMine))]
        private class WaitForUpdateRequest : MachineState { }

        [OnEventGotoState(typeof(eMonitorSuccess), typeof(Done))]
        [OnEventDoAction(typeof(eMonitorUpdateLiveness), nameof(AssertNotMine))]
        [OnEventDoAction(typeof(eMonitorResponseLiveness), nameof(CheckIfMine))]
        private class WaitForResponse : MachineState { }

        [IgnoreEvents(typeof(eMonitorUpdateLiveness),
                    typeof(eMonitorResponseLiveness))]
        private class Done : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[LivenessUpdatetoResponseMonitor] Initializing ...\n");

            MyRequestId = (this.ReceivedEvent as eInitialize).reqId;

            this.Raise(new eLocal());
        }

        private void CheckIfMine()
        {
            Console.WriteLine("[LivenessUpdatetoResponseMonitor] CheckIfMine ...\n");
            if ((this.ReceivedEvent as eMonitorUpdateLiveness).reqId == this.MyRequestId)
            {
                this.Raise(new eMonitorSuccess());
            }
        }

        private void AssertNotMine()
        {
            Console.WriteLine("[LivenessUpdatetoResponseMonitor] AssertNotMine ...\n");
            Assert(this.MyRequestId != (this.ReceivedEvent as eMonitorResponseLiveness).reqId, "LivenessUpdatetoResponse failed.");
        }
        #endregion
    }
}
