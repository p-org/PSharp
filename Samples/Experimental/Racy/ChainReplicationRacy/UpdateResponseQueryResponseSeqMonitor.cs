using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    /// <summary>
    /// Checks that a update(x, y) followed immediately by query(x) should return y.
    /// </summary>
    class UpdateResponseQueryResponseSeqMonitor : Machine
    {
        #region events
        private class eLocal : Event { }

        public class eMonitorResponseToUpdate : Event
        {
            public Tuple<MachineId, int, int> mrPayload;

            public eMonitorResponseToUpdate(Tuple<MachineId, int, int> mrPayload)
            {
                this.mrPayload = mrPayload;
            }
        }

        public class eMonitorResponseToQuery : Event
        {
            public Tuple<MachineId, int, int> mrqPayload;

            public eMonitorResponseToQuery(Tuple<MachineId, int, int> mrqPayload)
            {
                this.mrqPayload = mrqPayload;
            }
        }

        public class eMonitorUpdateServers : Event
        {
            public List<MachineId> musPayload;

            public eMonitorUpdateServers(List<MachineId> musPayload)
            {
                this.musPayload = musPayload;
            }
        }

        public class eStop : Event { }
        #endregion

        #region fields
        private List<MachineId> Servers;
        private Dictionary<int, int> LastUpdateResponse;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eLocal), typeof(Wait))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eMonitorResponseToUpdate), nameof(OnMonitorResponseToUpdate))]
        [OnEventGotoState(typeof(eGotoWait), typeof(Wait))]
        [OnEventDoAction(typeof(eMonitorResponseToQuery), nameof(OnMonitorResponseToQuery))]
        [OnEventDoAction(typeof(eMonitorUpdateServers), nameof(UpdateServers))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class Wait : MachineState { }

        private class eGotoWait : Event { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("[UpdateResponseQueryResponseSeqMonitor] Initializing ...\n");

            LastUpdateResponse = new Dictionary<int, int>();

            this.Raise(new eLocal());
        }

        private void UpdateServers()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Updating servers ...\n");
            this.Servers = (this.ReceivedEvent as eMonitorUpdateServers).musPayload;

            foreach (var server in this.Servers)
            {
                this.Send(server, new ChainReplicationServer.eInformAboutMonitor2(Id));
            }
        }

        private bool Contains(List<MachineId> seq, MachineId target)
        {
            for (int i = 0; i < this.Servers.Count; i++)
            {
                if (seq[i].Equals(target))
                {
                    return true;
                }
            }

            return false;
        }

        private void Stop()
        {
            Console.WriteLine("[UpdateResponseQueryResponseSeqMonitor] Stopping ...\n");

            Raise(new Halt());
        }

        private void OnMonitorResponseToUpdate()
        {
            Console.WriteLine("[UpdateResponseQueryResponseSeqMonitor] eMonitorResponseToUpdate ...\n");

            var tail = (this.ReceivedEvent as eMonitorResponseToUpdate).mrPayload.Item1;
            var key = (this.ReceivedEvent as eMonitorResponseToUpdate).mrPayload.Item2;
            var value = (this.ReceivedEvent as eMonitorResponseToUpdate).mrPayload.Item3;

            if (this.Contains(this.Servers, tail))
            {
                if (this.LastUpdateResponse.ContainsKey(key))
                {
                    this.LastUpdateResponse[key] = value;
                }
                else
                {
                    this.LastUpdateResponse.Add(key, value);
                }
            }

            Raise(new eGotoWait());
        }

        private void OnMonitorResponseToQuery()
        {
            Console.WriteLine("[UpdateResponseQueryResponseSeqMonitor] eMonitorResponseToQuery ...\n");

            var tail = (this.ReceivedEvent as eMonitorResponseToQuery).mrqPayload.Item1;
            var key = (this.ReceivedEvent as eMonitorResponseToQuery).mrqPayload.Item2;
            var value = (this.ReceivedEvent as eMonitorResponseToQuery).mrqPayload.Item3;

            if (this.Contains(this.Servers, tail))
            {
                Assert(value == this.LastUpdateResponse[key], "Value {0} is not " +
                    "equal to {1}", value, this.LastUpdateResponse[key]);
            }

            Raise(new eGotoWait());
        }
        #endregion
    }
}
