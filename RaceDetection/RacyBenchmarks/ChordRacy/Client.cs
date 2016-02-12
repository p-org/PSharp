using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChordRacy
{
    class Client : Machine
    {
        #region events
        private class eLocal : Event { }

        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public Tuple<MachineId, List<int>> initPayload;

            public eInitialize(Tuple<MachineId, List<int>> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        public class eNotifyClient : Event { }

        public class eQueryIdResp : Event
        {
            public int succId;

            public eQueryIdResp(int succId)
            {
                this.succId = succId;
            }
        }

        public class eFindSuccessorResp : Event
        {
            public ChordNode.Message msg;

            public eFindSuccessorResp(ChordNode.Message msg)
            {
                this.msg = msg;
            }
        }

        public class eStop : Event { }
        #endregion

        #region fields
        private MachineId Cluster;
        private List<int> Keys;
        private int QueryKey;

        private int QueryCounter;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(Waiting))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnWaitingEntry))]
        [OnEventGotoState(typeof(eNotifyClient), typeof(Querying))]
        [OnEventGotoState(typeof(eLocal), typeof(Querying))]
        [OnEventDoAction(typeof(eQueryIdResp), nameof(ReceiveSuccessorId))]
        [OnEventDoAction(typeof(eFindSuccessorResp), nameof(SuccessorFound))]
        [OnEventDoAction(typeof(eStop), nameof(Stopping))]
        private class Waiting : MachineState { }

        [OnEntry(nameof(OnQueryingEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(Waiting))]
        private class Querying : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[Client] Initializing ...\n");

            Cluster = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            Keys = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            QueryCounter = 0;

            this.Raise(new eLocal());
        }

        private void OnWaitingEntry()
        {
            Console.WriteLine("[Client] Waiting ...\n");
        }

        private void OnQueryingEntry()
        {
            if (QueryCounter < 3)
            {
                Console.WriteLine("[Client] Querying ...\n");

                Random random = new Random(0);
                var randomValue = random.Next(Keys.Count);
                QueryKey = Keys[randomValue];

                this.Send(Cluster, new Cluster.eFindSuccessor(new Tuple<MachineId, int, int>(Id, QueryKey, -1)));

                QueryCounter++;
            }

            this.Raise(new eLocal());
        }

        private void ReceiveSuccessorId()
        {
            var id = (this.ReceivedEvent as eQueryIdResp).succId;

            Console.WriteLine("[Client] Received successor with Id {0} for Key {1}  ...\n",
                id, this.QueryKey);

            this.Raise(new eLocal());
        }

        private void SuccessorFound()
        {
            Console.WriteLine("[Client] Successor found  ...\n");

            var successor = (this.ReceivedEvent as eFindSuccessorResp).msg.Machine;
            var id = (this.ReceivedEvent as eFindSuccessorResp).msg.Id;
            this.Send(successor, new ChordNode.eQueryId(Id));
        }

        private void Stopping()
        {
            Console.WriteLine("[Client] Stopping ...\n");
            Raise(new Halt());
        }
        #endregion
    }
}
