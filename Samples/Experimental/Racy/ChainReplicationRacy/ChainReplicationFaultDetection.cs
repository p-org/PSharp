using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    class ChainReplicationFaultDetection : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public Tuple<MachineId, List<MachineId>> initPayload;

            public eInitialize(Tuple<MachineId, List<MachineId>> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        private class eLocal : Event { }

        private class eGotoStartMonitoring : Event { }

        public class eCRPong : Event { }

        public class eTimeout : Event { }

        public class eCancelTimerSuccess : Event { }

        public class eFaultCorrected : Event
        {
            public List<MachineId> fcPayload;

            public eFaultCorrected(List<MachineId> fcPayload)
            {
                this.fcPayload = fcPayload;
            }
        }
        #endregion

        #region fields
        private List<MachineId> Servers;
        private MachineId Master;
        private MachineId Timer;

        private int CheckNodeIdx;
        private int Faults;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(StartMonitoring))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnStartMonitoringEntry))]
        [OnEventDoAction(typeof(eCRPong), nameof(OnCRPong))]
        [OnEventGotoState(typeof(eGotoStartMonitoring), typeof(StartMonitoring))]
        [OnEventGotoState(typeof(eTimeout), typeof(HandleFailure))]
        private class StartMonitoring : MachineState { }

        [OnEntry(nameof(OnCancelTimerEntry))]
        [OnEventDoAction(typeof(eTimeout), nameof(CallReturn))]
        [OnEventDoAction(typeof(eCancelTimerSuccess), nameof(CallReturn))]
        private class CancelTimer : MachineState { }

        [OnEntry(nameof(OnHandleFailureEntry))]
        [IgnoreEvents(typeof(eCRPong))]
        [OnEventDoAction(typeof(eFaultCorrected), nameof(OnFaultCorrected))]
        [OnEventGotoState(typeof(eGotoStartMonitoring), typeof(StartMonitoring))]
        private class HandleFailure : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[FaultDetection] Initializing ...\n");

            CheckNodeIdx = 0;
            Faults = 100;

            Master = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            Servers = (this.ReceivedEvent as eInitialize).initPayload.Item2;

            this.Raise(new eLocal());
        }

        private void OnStartMonitoringEntry()
        {
            if (Faults < 1)
            {
                DoGlobalAbort();
                return;
            }

            Console.WriteLine("[FaultDetection] StartMonitoring ...\n");

            //Console.WriteLine("{0} sending event {1} to {2}\n",
            //    machine, typeof(eStartTimer), machine.Timer);
            //this.Send(machine.Timer, new eStartTimer());

            //Console.WriteLine("{0} sending event {1} to {2}\n",
            //    machine, typeof(eCRPing), machine.Servers[machine.CheckNodeIdx]);
            //this.Send(machine.Servers[machine.CheckNodeIdx], new eCRPing(machine));

            BoundedFailureInjection();
            Faults--;
        }

        private void OnCancelTimerEntry()
        {
            Console.WriteLine("[FaultDetection] CancelTimer ...\n");

            Console.WriteLine("{0} sending event {1} to {2}\n",
                this, typeof(Timer.eCancelTimer), Timer);
            this.Send(Timer, new Timer.eCancelTimer());
        }

        private void OnHandleFailureEntry()
        {
            Console.WriteLine("[FaultDetection] HandleFailure ...\n");

            Console.WriteLine("{0} sending event {1} to {2}\n",
                this, typeof(ChainReplicationMaster.eFaultDetected), Master);
            this.Send(Master, new ChainReplicationMaster.eFaultDetected(Servers[CheckNodeIdx]));
        }


        private void CallReturn()
        {
            Console.WriteLine("[FaultDetection] CancelTimer (return) ...\n");
        }

        private void BoundedFailureInjection()
        {
            Console.WriteLine("[FaultDetection] BoundedFailureInjection ...\n");

            if (this.Servers.Count > 1)
            {
                if (this.Random())
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        this, typeof(eTimeout), this);
                    this.Send(Id, new eTimeout());
                }
                else
                {
                    Console.WriteLine("{0} sending event {1} to {2}\n",
                        this, typeof(eCRPong), this);
                    this.Send(this.Id, new eCRPong());
                }
            }
            else
            {
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    this, typeof(eCRPong), this);
                this.Send(Id, new eCRPong());
            }
        }

        private void DoGlobalAbort()
        {
            this.Send(this.Master, new ChainReplicationMaster.eStop());

            this.Stop();
        }

        private void Stop()
        {
            Console.WriteLine("[FaultDetection] Stopping ...\n");

            Raise(new Halt());
        }

        private void OnCRPong()
        {
            this.CheckNodeIdx++;
            if (this.CheckNodeIdx == this.Servers.Count)
            {
                this.CheckNodeIdx = 0;
            }

            Raise(new eGotoStartMonitoring());
        }

        private void OnFaultCorrected()
        {
            this.CheckNodeIdx = 0;
            this.Servers = (this.ReceivedEvent as eFaultCorrected).fcPayload;
            Raise(new eGotoStartMonitoring());
        }
        #endregion
    }
}
