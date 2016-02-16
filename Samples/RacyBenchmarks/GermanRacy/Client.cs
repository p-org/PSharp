using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GermanRacy
{
    class Client : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        private class eLocal : Event { }

        public class eInitialize : Event
        {
            public Tuple<int, MachineId, bool> initPayload;

            public eInitialize(Tuple<int, MachineId, bool> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        private class eWait : Event { }

        private class eNormal : Event { }

        public class eAskShare : Event
        {
            public MachineId mId;

            public eAskShare(MachineId mId)
            {
                this.mId = mId;
            }
        }

        public class eAskExcl : Event
        {
            public MachineId mid;

            public eAskExcl(MachineId mid)
            {
                this.mid = mid;
            }
        }

        public class eInvalidate : Event { }

        public class eGrantExcl : Event { }

        public class eGrantShare : Event { }

        public class eStop : Event { }
        #endregion

        #region fields
        private int Identity;

        private MachineId Host;

        private bool Pending;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(onInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(Invalid))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnInvalidEntry))]
        [OnEventGotoState(typeof(eAskShare), typeof(AskedShare))]
        [OnEventGotoState(typeof(eAskExcl), typeof(AskedExcl))]
        [OnEventGotoState(typeof(eInvalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(eGrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(eGrantShare), typeof(Sharing))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class Invalid : MachineState { }

        [OnEntry(nameof(OnAskedShareEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(InvalidWaiting))]
        private class AskedShare : MachineState { }

        [OnEntry(nameof(OnAskedExclEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(InvalidWaiting))]
        private class AskedExcl : MachineState { }

        [OnEntry(nameof(OnInvalidWaitingEntry))]
        [DeferEvents(typeof(eAskShare),
                    typeof(eAskExcl))]
        [OnEventGotoState(typeof(eInvalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(eGrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(eGrantShare), typeof(Sharing))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class InvalidWaiting : MachineState { }

        [OnEntry(nameof(OnAskedEx2Entry))]
        [OnEventGotoState(typeof(eLocal), typeof(ShareWaiting))]
        private class AskedEx2 : MachineState { }

        [OnEntry(nameof(OnSharingEntry))]
        [OnEventGotoState(typeof(eInvalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(eGrantShare), typeof(Sharing))]
        [OnEventGotoState(typeof(eGrantExcl), typeof(Exclusive))]
        [OnEventGotoState(typeof(eAskExcl), typeof(AskedEx2))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        [OnEventDoAction(typeof(eAskShare), nameof(Ack))]
        private class Sharing : MachineState { }

        [OnEntry(nameof(OnShareWaitingEntry))]
        [OnEventGotoState(typeof(eInvalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(eGrantShare), typeof(Sharing))]
        [OnEventGotoState(typeof(eGrantExcl), typeof(Exclusive))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class ShareWaiting : MachineState { }

        [OnEntry(nameof(OnExclusiveEntry))]
        [IgnoreEvents(typeof(eAskShare),
                    typeof(eAskExcl))]
        [OnEventGotoState(typeof(eInvalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(eGrantShare), typeof(Sharing))]
        [OnEventGotoState(typeof(eGrantExcl), typeof(Exclusive))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class Exclusive : MachineState { }

        [OnEntry(nameof(OnInvalidatingEntry))]
        [OnEventGotoState(typeof(eWait), typeof(InvalidWaiting))]
        [OnEventGotoState(typeof(eNormal), typeof(Invalid))]
        private class Invalidating : MachineState { }
        #endregion

        #region actions
        private void onInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[Client] Initializing ...\n");

            Identity = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            Host = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            Pending = (this.ReceivedEvent as eInitialize).initPayload.Item3;

            this.Raise(new eLocal());
        }

        private void OnInvalidEntry()
        {
            Console.WriteLine("[Client] Invalid ...\n");
        }

        private void OnAskedShareEntry()
        {
            Console.WriteLine("[Client] AskedShare ...\n");

            var message = new Host.Message(Identity, Pending);
            this.Send(Host, new Host.eShareReq(message));
            Pending = message.Pending;

            this.Raise(new eLocal());
        }

        private void OnAskedExclEntry()
        {
            Console.WriteLine("[Client] AskedExcl ...\n");

            var message = new Host.Message(Identity, Pending);
            this.Send(Host, new Host.eExclReq(message));
            Pending = message.Pending;

            this.Raise(new eLocal());
        }

        private void OnInvalidWaitingEntry()
        {
            Console.WriteLine("[Client] InvalidWaiting ...\n");
        }

        private void OnAskedEx2Entry()
        {
            Console.WriteLine("[Client] AskedEx2 ...\n");

            var message = new Host.Message(Identity, Pending);
            this.Send(Host, new Host.eExclReq(message));
            Pending = message.Pending;

            this.Raise(new eLocal());
        }

        private void OnSharingEntry()
        {
            Console.WriteLine("[Client] Sharing ...\n");

            Pending = false;
        }

        private void OnShareWaitingEntry()
        {
            Console.WriteLine("[Client] ShareWaiting ...\n");
        }

        private void OnExclusiveEntry()
        {
            Console.WriteLine("[Client] Exclusive ...\n");

            Pending = false;
        }

        private void OnInvalidatingEntry()
        {
            Console.WriteLine("[Client] Invalidating ...\n");

            if (Pending)
            {
                this.Raise(new eWait());
            }
            else
            {
                this.Raise(new eNormal());
            }
        }

        private void Ack()
        {
            var cpu = (this.ReceivedEvent as eAskShare).mId;

            this.Send(cpu, new CPU.eAck());
        }

        private void Stop()
        {
            Console.WriteLine("[Client] Stopping ...\n");

            Raise(new Halt());
        }

        #endregion
    }
}
