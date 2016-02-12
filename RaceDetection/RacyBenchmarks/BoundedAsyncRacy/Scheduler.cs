using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoundedAsyncRacy
{
    class Scheduler : Machine
    {
        #region events
        private class eUnit : Event { }
        #endregion

        #region fields
        private MachineId Process1;
        private MachineId Process2;
        private MachineId Process3;
        private int Count;
        private int DoneCounter;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [DeferEvents(typeof(Process.eReq))]
        [OnEventGotoState(typeof(eUnit), typeof(Sync))]
        private class Init : MachineState { }

        [OnExit(nameof(OnSyncExit))]
        [OnEventGotoState(typeof(Process.eResp), typeof(Sync))]
        [OnEventGotoState(typeof(eUnit), typeof(Done))]
        [OnEventDoAction(typeof(Process.eReq), nameof(CountReq))]
        [OnEventDoAction(typeof(Process.eDone), nameof(CheckIfDone))]
        private class Sync : MachineState { }

        [OnEntry(nameof(OnDoneEntry))]
        private class Done : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("Initializing Scheduler");

            Process1 = CreateMachine(typeof(Process));
            Send(Process1, new Process.eInitialize(this.Id));

            Process2 = CreateMachine(typeof(Process));
            Send(Process2, new Process.eInitialize(this.Id));

            Process3 = CreateMachine(typeof(Process));
            Send(Process3, new Process.eInitialize(this.Id));

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(Process.eInit), Process1);
            this.Send(Process1, new Process.eInit(new Tuple<MachineId, MachineId>(Process2, Process3)));

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(Process.eInit), Process1);
            this.Send(Process2, new Process.eInit(new Tuple<MachineId, MachineId>(Process1, Process3)));

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(Process.eInit), Process1);
            this.Send(Process3, new Process.eInit(new Tuple<MachineId, MachineId>(Process1, Process2)));

            Count = 0;
            Console.WriteLine("Scheduler: Count: {0}", Count);

            DoneCounter = 3;

            Console.WriteLine("{0} raising event {1}", this, typeof(Process.eResp));
            this.Raise(new eUnit());
        }

        private void OnSyncExit()
        {
            Console.WriteLine("{0} sending event {1} to {2}",this, typeof(Process.eResp), Process1);
            this.Send(Process1, new Process.eResp());

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(Process.eResp), Process1);
            this.Send(Process2, new Process.eResp());

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(Process.eResp), Process1);
            this.Send(Process3, new Process.eResp());
        }

        private void OnDoneEntry()
        {
            Raise(new Halt());
        }

        private void CountReq()
        {
            this.Count++;
            Console.WriteLine("Scheduler: Count: {0}", this.Count);

            if (this.Count == 3)
            {
                this.Count = 0;
                Console.WriteLine("Scheduler: Count: {0}", this.Count);

                Console.WriteLine("{0} raising event {1}", this, typeof(Process.eResp));
                this.Raise(new Process.eResp());
            }
        }

        private void CheckIfDone()
        {
            this.DoneCounter--;

            if (this.DoneCounter == 0)
            {
                Console.WriteLine("Scheduler: Done");
                Raise(new Halt()) ;
            }
        }
        #endregion
    }
}