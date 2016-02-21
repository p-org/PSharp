using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GermanRacy
{
    class CPU : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        private class eLocal : Event { }

        public class eInitialize : Event
        {
            public Tuple<MachineId, List<MachineId>> initPayload;

            public eInitialize(Tuple<MachineId, List<MachineId>> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        public class eAck : Event { }
        #endregion

        #region fields
        private MachineId Host;
        private List<MachineId> Cache;

        private int QueryCounter;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(Requesting))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnRequestingEntry))]
        [OnEventGotoState(typeof(eAck), typeof(Requesting))]
        private class Requesting : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[CPU] Initializing ...\n");

            Host = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            Cache = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            QueryCounter = 0;

            this.Raise(new eLocal());
        }

        private void OnRequestingEntry()
        {
            Console.WriteLine("[CPU] Sending request {0} ...\n", QueryCounter);

            if (Random())
            {
                if (Random())
                {
                    this.Send(Cache[0], new Client.eAskShare(Id));
                }
                else
                {
                    this.Send(Cache[0], new Client.eAskExcl(Id));
                }
            }
            else if (Random())
            {
                if (Random())
                {
                    this.Send(Cache[1], new Client.eAskShare(Id));
                }
                else
                {
                    this.Send(Cache[1], new Client.eAskExcl(Id));
                }
            }
            else
            {
                if (Random())
                {
                    this.Send(Cache[2], new Client.eAskShare(Id));
                }
                else
                {
                    this.Send(Cache[2], new Client.eAskExcl(Id));
                }
            }

            QueryCounter++;

            if (QueryCounter == 2)
            {
                Console.WriteLine("[CPU] Stopping ...\n");

                this.Send(Host, new Host.eStop());

                foreach (var c in Cache)
                {
                    this.Send(c, new Client.eStop());
                }

                Raise(new Halt());
            }
        }
        #endregion
    }
}


