using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    /// <summary>
    /// The Client machine checks that for a configuration of 3 nodes
    /// an update(k,v) is followed by a successful query(k) == v. Also
    /// a random query is performed in the end.
    /// </summary>
    class Client : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public Tuple<int, MachineId, MachineId, int> initPayload;

            public eInitialize(Tuple<int, MachineId, MachineId, int> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        private class eLocal : Event { }

        private class eDone : Event { }

        private class eGotoPumpQueryRequests : Event { }

        private class eGotoPumpUpdateRequests : Event { }

        public class eStop : Event { }

        public class eResponseToUpdate : Event { }

        public class eResponseToQuery : Event
        {
            public Tuple<MachineId, int> payload;

            public eResponseToQuery()
            {
                this.payload = null;
            }

            public eResponseToQuery(Tuple<MachineId, int> payload)
            {
                this.payload = payload;
            }
        }

        public class eUpdateHeadTail : Event
        {
            public Tuple<MachineId, MachineId> pl;

            public eUpdateHeadTail(Tuple<MachineId, MachineId> pl)
            {
                this.pl = pl;
            }
        }
        #endregion

        #region fields
        private int Next;
        private MachineId HeadNode;
        private MachineId TailNode;
        private int StartIn;
        private Dictionary<int, int> KeyValue;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(PumpUpdateRequests))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnPumpUpdateRequestsEntry))]
        [IgnoreEvents(typeof(eResponseToUpdate))]
        [OnEventDoAction(typeof(eDone), nameof(OnPumpUpdateRequestsDone))]
        [OnEventGotoState(typeof(eGotoPumpQueryRequests), typeof(PumpQueryRequests))]
        [OnEventDoAction(typeof(eLocal), nameof(OnLocal))]
        [OnEventGotoState(typeof(eGotoPumpUpdateRequests), typeof(PumpUpdateRequests))]
        private class PumpUpdateRequests : MachineState { }

        [OnEntry(nameof(OnPumpQueryRequestsEntry))]
        [IgnoreEvents(typeof(eResponseToQuery))]
        [OnEventGotoState(typeof(eDone), typeof(End))]
        [OnEventDoAction(typeof(eLocal), nameof(OnLocal2))]
        [OnEventGotoState(typeof(eGotoPumpQueryRequests), typeof(PumpQueryRequests))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class PumpQueryRequests : MachineState { }

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
           Console.WriteLine("[Client-{0}] Initializing ...\n", Id);

            Next = 1;

            HeadNode = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            TailNode = (this.ReceivedEvent as eInitialize).initPayload.Item3;
            StartIn = (this.ReceivedEvent as eInitialize).initPayload.Item4;

            KeyValue = new Dictionary<int, int>();
            KeyValue.Add(1 * StartIn, 100);
            KeyValue.Add(2 * StartIn, 200);
            KeyValue.Add(3 * StartIn, 300);
            KeyValue.Add(4 * StartIn, 400);

            this.Raise(new eLocal());
        }

        private void OnPumpUpdateRequestsEntry()
        {
            Console.WriteLine("[Client-{0}] PumpUpdateRequests ...\n", Id);

            Console.WriteLine("{0} sending event {1} to {2}\n", this, typeof(ChainReplicationServer.eUpdate), HeadNode);
            this.Send(HeadNode, new ChainReplicationServer.eUpdate(new Tuple<MachineId, Tuple<int, int>>
                (this.Id, new Tuple<int, int>(Next * StartIn, KeyValue[Next * StartIn]))));

            if (Next >= 3)
            {
                this.Raise(new eDone());
            }
            else
            {
                this.Raise(new eLocal());
            }
        }

        private void OnPumpQueryRequestsEntry()
        {
            Console.WriteLine("[Client-{0}] PumpQueryRequests ...\n", Id);

            Console.WriteLine("{0} sending event {1} to {2}\n", this, typeof(ChainReplicationServer.eQuery), TailNode);
            this.Send(TailNode, new ChainReplicationServer.eQuery(new Tuple<MachineId, int>(this.Id, Next * StartIn)));

            if (Next >= 3)
            {
                this.Raise(new eDone());
            }
            else
            {
                this.Raise(new eLocal());
            }
        }

        private void OnEndEntry()
        {
            Console.WriteLine("[Client-{0}] End ...\n", Id);
            Raise(new Halt());
        }

        private void Stop()
        {
            Console.WriteLine("[Client-{0}] Stopping ...\n", this.Id);

            Raise(new Halt());
        }

        private void OnPumpUpdateRequestsDone()
        {
            this.Next = 1;
            Raise(new eGotoPumpQueryRequests());
        }

        private void OnLocal()
        {
            this.Next++;
            Raise(new eGotoPumpUpdateRequests());
        }

        private void OnLocal2()
        {
            this.Next++;
            Raise(new eGotoPumpQueryRequests());
        }
        #endregion
    }
}