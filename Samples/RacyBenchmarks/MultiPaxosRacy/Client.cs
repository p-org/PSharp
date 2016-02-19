using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace MultiPaxosRacy
{
    class Client : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        private class eLocal : Event { }

        public class eInitialize : Event
        {
            public Tuple<List<MachineId>, MachineId> initPayload;

            public eInitialize(Tuple<List<MachineId>, MachineId> initPayload)
            {
                this.initPayload = initPayload;
            }
        }
        #endregion

        #region fields
        private List<MachineId> Servers;

        private MachineId ValidityMonitor;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(PumpRequestOne))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnPumpRequestOneEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(End))]
        private class PumpRequestOne : MachineState { }

        [OnEntry(nameof(OnEndEntry))]
        private class End : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[Client] Initializing ...\n");

            Servers = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            ValidityMonitor = (this.ReceivedEvent as eInitialize).initPayload.Item2;

            this.Raise(new eLocal());
        }

        private void OnPumpRequestOneEntry()
        {
            Console.WriteLine("[Client] Pumping first request ...\n");

            Console.WriteLine("{0} sending event {1} to {2}\n", this,
                    typeof(ValidityCheckMonitor.eMonitorClientSent), typeof(ValidityCheckMonitor));
            this.Send(ValidityMonitor, new ValidityCheckMonitor.eMonitorClientSent(1));

            if (Random())
            {
                Console.WriteLine("{0} sending event {1} to {2}-{3}\n", this,
                    typeof(eUpdate), Servers[0], 0);
                this.Send(Servers[0], new eUpdate(new Tuple<int, int>(0, 1)));
            }
            else
            {
                Console.WriteLine("{0} sending event {1} to {2}-{3}\n", this, typeof(eUpdate),
                    Servers[Servers.Count - 1], Servers.Count - 1);
                this.Send(Servers[Servers.Count - 1], new eUpdate(new Tuple<int, int>(0, 1)));
            }

            this.Raise(new eLocal());
        }

        private void OnEndEntry()
        {
            Console.WriteLine("[Client] Stopping ...\n");

            Raise(new Halt());
        }
        #endregion
    }
}
