using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    class LivenessQuerytoResponseMonitor : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        private class eLocal : Event { }

        public class eInitialize : Event
        {
            public int reqId;

            public eInitialize(int reqId)
            {
                this.reqId = reqId;
            }
        }

        public class eMonitorSuccess : Event { }

        public class eMonitorQueryLiveness : Event
        {
            public int reqId;

            public eMonitorQueryLiveness(int reqId)
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
        [OnEventGotoState(typeof(eLocal), typeof(WaitForQueryRequest))]
        private class WaitingForInit : MachineState { }

        [OnEventGotoState(typeof(eMonitorSuccess), typeof(WaitForResponse))]
        [OnEventDoAction(typeof(eMonitorQueryLiveness), nameof(CheckIfMine))]
        [OnEventDoAction(typeof(eMonitorResponseLiveness), nameof(AssertNotMine))]
        private class WaitForQueryRequest : MachineState { }

        [OnEventGotoState(typeof(eMonitorSuccess), typeof(Done))]
        [OnEventDoAction(typeof(eMonitorQueryLiveness), nameof(AssertNotMine))]
        [OnEventDoAction(typeof(eMonitorResponseLiveness), nameof(CheckIfMine))]
        private class WaitForResponse : MachineState { }

        [IgnoreEvents(typeof(eMonitorQueryLiveness),
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
            Console.WriteLine("[LivenessQuerytoResponseMonitor] Initializing ...\n");

            MyRequestId = (this.ReceivedEvent as eInitialize).reqId;

            this.Raise(new eLocal());
        }

        private void CheckIfMine()
        {
            Console.WriteLine("[LivenessQuerytoResponseMonitor] CheckIfMine ...\n");
            int receivedReqId;
            try
            {
                receivedReqId = (this.ReceivedEvent as eMonitorQueryLiveness).reqId;
            }
            catch (Exception ex)
            {
                receivedReqId = (this.ReceivedEvent as eMonitorResponseLiveness).reqId;
            }
            if (receivedReqId == this.MyRequestId)
            {
                this.Raise(new eMonitorSuccess());
            }
        }

        private void AssertNotMine()
        {
            Console.WriteLine("[LivenessQuerytoResponseMonitor] AssertNotMine ...\n");
            int receivedReqId;
            try
            {
                receivedReqId = (this.ReceivedEvent as eMonitorQueryLiveness).reqId;
            }
            catch (Exception ex)
            {
                receivedReqId = (this.ReceivedEvent as eMonitorResponseLiveness).reqId;
            }
            Assert(this.MyRequestId != receivedReqId, "LivenessQuerytoResponse failed.");
        }
        #endregion
    }
}