using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    class Timer : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public MachineId target;

            public eInitialize(MachineId target)
            {
                this.target = target;
            }
        }

        private class eLocal : Event { }

        private class eGotoLoop : Event { }

        public class eCancelTimer : Event { }

        public class eStartTimer : Event { }
        #endregion

        #region fields
        private MachineId Target;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(Loop))]
        private class WaitingForInit : Event { }

        [IgnoreEvents(typeof(eCancelTimer))]
        [OnEventGotoState(typeof(eStartTimer), typeof(Started))]
        private class Loop : MachineState { }

        [OnEntry(nameof(OnStartedEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(Loop))]
        [OnEventDoAction(typeof(eCancelTimer), nameof(OnCancelTimer))]
        [OnEventGotoState(typeof(eGotoLoop), typeof(Loop))]
        private class Started : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[Timer] Initializing ...\n");

            Target = (this.ReceivedEvent as eInitialize).target;

            this.Raise(new eLocal());
        }

        private void OnStartedEntry()
        {
            Console.WriteLine("[Timer] Started ...\n");

            if (this.Random())
            {
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    this, typeof(ChainReplicationFaultDetection.eTimeout), Target);
                this.Send(Target, new ChainReplicationFaultDetection.eTimeout());
                this.Raise(new eLocal());
            }
        }

        private void OnCancelTimer()
        {
            Console.WriteLine("{0} sending event {1} to {2}\n", this, typeof(ChainReplicationFaultDetection.eCancelTimerSuccess), this.Target);
            this.Send(this.Target, new ChainReplicationFaultDetection.eCancelTimerSuccess());

            Raise(new eGotoLoop());
        }
        #endregion
    }
}
