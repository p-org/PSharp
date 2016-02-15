using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GermanRacy
{
    class Host : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        private class eLocal : Event { }

        private class eGrant : Event { }

        private class eNeedInvalidate : Event { }

        public class eInitialize : Event
        {
            public int initPayload;

            public eInitialize(int initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        public class eInvalidateAck : Event { }

        public class eShareReq : Event
        {
            public Message mPayload;

            public eShareReq(Message mPayload)
            {
                this.mPayload = mPayload;
            }
        }

        public class eExclReq : Event
        {
            public Message ePayload;

            public eExclReq(Message ePayload)
            {
                this.ePayload = ePayload;
            }
        }

        public class eStop : Event { }
        #endregion

        #region classes
        internal class Message
        {
            public int Id;
            public bool Pending;

            public Message(int id, bool pending)
            {
                this.Id = id;
                this.Pending = pending;
            }
        }

        #endregion

        #region fields
        private List<MachineId> Clients;
        private MachineId CPU;
        private MachineId CurrentClient;

        private List<MachineId> SharerList;

        private bool isCurrReqExcl;
        private bool isExclGranted;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(Receiving))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnReceivingEntry))]
        [DeferEvents(typeof(eInvalidateAck))]
        [OnEventGotoState(typeof(eShareReq), typeof(ShareRequest))]
        [OnEventGotoState(typeof(eExclReq), typeof(ExclRequest))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class Receiving : MachineState { }

        [OnEntry(nameof(OnShareRequestEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(Processing))]
        private class ShareRequest : MachineState { }

        [OnEntry(nameof(OnExclRequestEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(Processing))]
        private class ExclRequest : MachineState { }

        [OnEntry(nameof(OnProcessingEntry))]
        [OnEventGotoState(typeof(eNeedInvalidate), typeof(CheckingInvariant))]
        [OnEventGotoState(typeof(eGrant), typeof(GrantingAccess))]
        private class Processing : MachineState { }

        [OnEntry(nameof(OnGrantingAccessEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(Receiving))]
        private class GrantingAccess : MachineState { }

        [OnEntry(nameof(OnCheckingInvariantEntry))]
        [DeferEvents(typeof(eShareReq),
                    typeof(eExclReq),
                    typeof(eStop))]
        [OnEventGotoState(typeof(eGrant), typeof(GrantingAccess))]
        [OnEventDoAction(typeof(eInvalidateAck), nameof(RecAck))]
        private class CheckingInvariant : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            var n = (this.ReceivedEvent as eInitialize).initPayload;

            Console.WriteLine("[Host] Initializing ...\n");

            Clients = new List<MachineId>();
            SharerList = new List<MachineId>();
            CurrentClient = null;

            for (int idx = 0; idx < n; idx++)
            {
                MachineId clId = CreateMachine(typeof(Client));
                Clients.Add(clId);
                Send(clId, new Client.eInitialize(new Tuple<int, MachineId, bool>(idx, Id, false)));
            }

            CPU = CreateMachine(typeof(CPU));
            Send(CPU, new CPU.eInitialize(new Tuple<MachineId, List<MachineId>>(Id, Clients)));
            Assert(SharerList.Count == 0);

            this.Raise(new eLocal());
        }

        private void OnReceivingEntry()
        {
            Console.WriteLine("[Host] Receiving ...\n");
        }

        private void OnShareRequestEntry()
        {
            Console.WriteLine("[Host] ShareRequest ...\n");

            var id = (this.ReceivedEvent as eShareReq).mPayload.Id;
            (this.ReceivedEvent as eShareReq).mPayload.Pending = true;
            CurrentClient = Clients[id];
            isCurrReqExcl = false;

            this.Raise(new eLocal());
        }

        private void OnExclRequestEntry()
        {
            Console.WriteLine("[Host] ExclRequest ...\n");

            var id = (this.ReceivedEvent as eExclReq).ePayload.Id;
            (this.ReceivedEvent as eExclReq).ePayload.Pending = true;
            CurrentClient = Clients[id];
            isCurrReqExcl = true;

            this.Raise(new eLocal());
        }

        private void OnProcessingEntry()
        {
            Console.WriteLine("[Host] Processing ...\n");

            if (isCurrReqExcl || isExclGranted)
            {
                this.Raise(new eNeedInvalidate());
            }
            else
            {
                this.Raise(new eGrant());
            }
        }

        private void OnGrantingAccessEntry()
        {
            Console.WriteLine("[Host] GrantingAccess ...\n");

            if (isCurrReqExcl)
            {
                isExclGranted = true;
                this.Send(CurrentClient, new Client.eGrantExcl());
            }
            else
            {
                this.Send(CurrentClient, new Client.eGrantShare());
            }

            SharerList.Add(CurrentClient);

            this.Send(CPU, new CPU.eAck());

            this.Raise(new eLocal());
        }

        private void OnCheckingInvariantEntry()
        {
            Console.WriteLine("[Host] CheckingInvariant ...\n");

            if (SharerList.Count == 0)
            {
                this.Raise(new eGrant());
            }
            else
            {
                foreach (var sharer in SharerList)
                {
                    this.Send(sharer, new Client.eInvalidate());
                }
            }
        }

        private void RecAck()
        {
            Console.WriteLine("[Host] RecAck ...\n");

            this.SharerList.RemoveAt(0);
            if (this.SharerList.Count == 0)
            {
                this.Raise(new eGrant());
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Host] Stopping ...\n");

            Raise(new Halt());
        }
        #endregion
    }
}
