using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoundedAsyncRacy
{
    class Process : Machine
    {
        #region events
        private class eWaitForInitialization : Event { }
        #endregion

        #region structs
        internal class CountMessage
        {
            public int Count;

            public CountMessage(int count)
            {
                this.Count = count;
            }
        }

        public class eInitialize : Event
        {
            public MachineId schedulerId;

            public eInitialize(MachineId schedulerId)
            {
                this.schedulerId = schedulerId;
            }
        }

        private class eUnit : Event { }

        public class eInit : Event
        {
            public Tuple<MachineId, MachineId> initPayload;

            public eInit(Tuple<MachineId, MachineId> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        public class eMyCount : Event
        {
            public CountMessage cntPayload;
            public eMyCount(CountMessage cntPayload)
            {
                this.cntPayload = cntPayload;
            }
        }


        public class eResp : Event { }

        public class eReq : Event { }

        public class eDone : Event { }
        #endregion

        #region fields
        private MachineId Scheduler;
        private MachineId LeftProcess;
        private MachineId RightProcess;

        private CountMessage countMessage;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInitialization), typeof(WaitingForInitialization))]
        private class _Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [DeferEvents(typeof(eInit))]
        [OnEventGotoState(typeof(eUnit), typeof(Init))]
        private class WaitingForInitialization : MachineState { }

        [OnEntry(nameof(OnInit2))]
        [OnEventGotoState(typeof(eMyCount), typeof(Init))]
        [OnEventGotoState(typeof(eResp), typeof(SendCount))]
        [OnEventDoAction(typeof(eInit), nameof(InitAction))]
        private class Init : MachineState { }

        [OnEntry(nameof(OnSendCountEntry))]
        [OnEventGotoState(typeof(eUnit), typeof(Done))]
        [OnEventGotoState(typeof(eResp), typeof(SendCount))]
        [OnEventDoAction(typeof(eMyCount), nameof(ConfirmThatInSync))]
        private class SendCount : MachineState { }

        [OnEntry(nameof(OnDoneEntry))]
        [DeferEvents(typeof(eResp), typeof(eMyCount))]
        private class Done : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInitialization());
        }

        private void OnInitialize()
        {
            Console.WriteLine("Initializing Process");

            Scheduler = (this.ReceivedEvent as eInitialize).schedulerId;

            Console.WriteLine("{0} raising event {1}", this, typeof(eUnit));
            this.Raise(new eUnit());
        }

        private void OnInit2()
        {
            countMessage = new CountMessage(0);
            Console.WriteLine("Process: Count: {0}", countMessage.Count);
        }

        private void OnSendCountEntry()
        {
            countMessage.Count = countMessage.Count + 1;
            Console.WriteLine("Process: Count: {0}", countMessage.Count);

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eMyCount), LeftProcess);
            this.Send(LeftProcess, new eMyCount(countMessage));

            countMessage.Count = countMessage.Count + 1;

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eMyCount), RightProcess);
            this.Send(RightProcess, new eMyCount(countMessage));

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eReq), Scheduler);
            this.Send(Scheduler, new eReq());

            if (countMessage.Count > 10)
            {
                this.Raise(new eUnit());
            }
        }

        private void OnDoneEntry()
        {
            Console.WriteLine("Process: Done");
            this.Send(Scheduler, new eDone());
            Raise(new Halt());
        }

        private void InitAction()
        {
            Console.WriteLine("Process: Performing InitAction");

            this.LeftProcess = (this.ReceivedEvent as eInit).initPayload.Item1;
            this.RightProcess = (this.ReceivedEvent as eInit).initPayload.Item2;

            Console.WriteLine("{0} sending event {1} to {2}", this, typeof(eReq), this.Scheduler);
            this.Send(this.Scheduler, new eReq());
        }

        private void ConfirmThatInSync()
        {
            Console.WriteLine("Process: Performing ConfirmThatInSync");

            var countMsg = (this.ReceivedEvent as eMyCount).cntPayload;

            Assert((countMessage.Count <= countMsg.Count) &&
                (countMessage.Count >= (countMsg.Count - 1)), "Caught!!");
        }

        #endregion
    }
}


