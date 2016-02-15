using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPaxosRacy
{
    class Timer : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public MachineId Target;

            public eInitialize(MachineId Target)
            {
                this.Target = Target;
            }
        }

        private class eLocal : Event { }

        public class eCancelTimer : Event { }

        public class eStartTimer : Event { }

        public class eTimeout : Event { }

        public class eStop : Event { }
        #endregion

        #region fields
        private MachineId Target;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(Initialize))]
        [IgnoreEvents(typeof(eCancelTimer))]
        [DeferEvents(typeof(eStartTimer))]
        [OnEventGotoState(typeof(eLocal), typeof(Loop))]
        private class WaitingForInit : MachineState { }

        [IgnoreEvents(typeof(eCancelTimer))]
        [OnEventGotoState(typeof(eStartTimer), typeof(Started))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class Loop : MachineState { }

        [OnEntry(nameof(OnStartedEntry))]
        [IgnoreEvents(typeof(eStartTimer))]
        [OnEventGotoState(typeof(eLocal), typeof(Loop))]
        [OnEventGotoState(typeof(eCancelTimer), typeof(Loop))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class Started : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("[Timer] Initializing ...\n");
            this.Raise(new eWaitForInit());
        }

        private void Initialize()
        {
            this.Target = (this.ReceivedEvent as eInitialize).Target;
            this.Raise(new eLocal());
        }

        private void OnStartedEntry()
        {
            Console.WriteLine("[Timer] Started ...\n");

            if (this.Random())
            {
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    this, typeof(eTimeout), this.Target);
                this.Send(this.Target, new eTimeout());
                this.Raise(new eLocal());
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Timer] Stopping ...\n");
            Raise(new Halt());
        }
        #endregion
    }
}

