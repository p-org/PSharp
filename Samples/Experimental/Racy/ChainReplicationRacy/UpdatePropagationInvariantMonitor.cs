using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    /// <summary>
    /// This monitor checks the Update Propagation Invariant 
    /// Invariant 1: HISTj <= HISTi forall i <= j
    /// Invariant 2: HISTi = HISTj + SENTi
    /// </summary>
    class UpdatePropagationInvariantMonitor : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public List<MachineId> initPayload;

            public eInitialize(List<MachineId> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        private class eLocal : Event { }

        public class eMonitorHistoryUpdate : Event
        {
            public Tuple<MachineId, List<int>> mPayload;

            public eMonitorHistoryUpdate(Tuple<MachineId, List<int>> mPayload)
            {
                this.mPayload = mPayload;
            }
        }

        public class eMonitorSentUpdate : Event
        {
            public Tuple<MachineId, List<Tuple<int, MachineId, Tuple<int, int>>>> msPayload;

            public eMonitorSentUpdate(Tuple<MachineId, List<Tuple<int, MachineId, Tuple<int, int>>>> msPayload)
            {
                this.msPayload = msPayload;
            }
        }

        public class eMonitorUpdateServers : Event
        {
            public List<MachineId> uPayload;

            public eMonitorUpdateServers(List<MachineId> uPayload)
            {
                this.uPayload = uPayload;
            }
        }

        public class eStop : Event { }
        #endregion

        #region fields
        private List<MachineId> Servers;

        private Dictionary<MachineId, List<int>> HistoryMap;
        private Dictionary<MachineId, List<int>> SentMap;
        private List<int> TempSeq;

        private MachineId Next;
        private MachineId Prev;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(WaitForUpdateMessage))]
        private class WaitingForInit : MachineState { }

        [OnEventDoAction(typeof(eMonitorHistoryUpdate), nameof(CheckInvariant1))]
        [OnEventDoAction(typeof(eMonitorSentUpdate), nameof(CheckInvariant2))]
        [OnEventDoAction(typeof(eMonitorUpdateServers), nameof(UpdateServers))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class WaitForUpdateMessage : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Initializing ...\n");

            HistoryMap = new Dictionary<MachineId, List<int>>();
            SentMap = new Dictionary<MachineId, List<int>>();
            TempSeq = new List<int>();

            Servers = (this.ReceivedEvent as eInitialize).initPayload;

            foreach (var server in Servers)
            {
                this.Send(server, new ChainReplicationServer.eInformAboutMonitor1(Id));
            }

            this.Raise(new eLocal());
        }

        private void CheckInvariant1()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Checking invariant 1 ...\n");

            var target = (this.ReceivedEvent as eMonitorHistoryUpdate).mPayload.Item1;
            var history = (this.ReceivedEvent as eMonitorHistoryUpdate).mPayload.Item2;

            Console.WriteLine(target);
            Console.WriteLine("checking IsSorted in CheckInvariant1");
            this.IsSorted(history);

            if (this.HistoryMap.ContainsKey(target))
            {
                this.HistoryMap[target] = history;
            }
            else
            {
                this.HistoryMap.Add(target, history);
            }

            // HIST(i+1) <= HIST(i)
            this.GetNext(target);
            if (this.Next != null && this.HistoryMap.ContainsKey(this.Next))
            {
                this.CheckLessThan(this.HistoryMap[this.Next], this.HistoryMap[target]);
            }

            // HIST(i) <= HIST(i-1)
            this.GetPrev(target);
            if (this.Prev != null && this.HistoryMap.ContainsKey(this.Prev))
            {
                this.CheckLessThan(this.HistoryMap[target], this.HistoryMap[this.Prev]);
            }
        }

        private void CheckInvariant2()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Checking invariant 2 ...\n");

            this.ClearTempSeq();

            var target = (this.ReceivedEvent as eMonitorSentUpdate).msPayload.Item1;
            var seq = (this.ReceivedEvent as eMonitorSentUpdate).msPayload.Item2;

            this.ExtractSeqId(seq);

            if (this.SentMap.ContainsKey(target))
            {
                this.SentMap[target] = this.TempSeq;
            }
            else
            {
                this.SentMap.Add(target, this.TempSeq);
            }

            this.ClearTempSeq();

            // HIST(i) = HIST(i+1) + SENT(i)
            this.GetNext(target);
            if (this.Next != null && this.HistoryMap.ContainsKey(this.Next))
            {
                this.MergeSeq(this.HistoryMap[this.Next], this.SentMap[target]);
                this.CheckEqual(this.HistoryMap[target], this.TempSeq);
            }

            this.ClearTempSeq();

            // HIST(i-1) = HIST(i) + SENT(i-1)
            this.GetPrev(target);
            if (this.Prev != null && this.HistoryMap.ContainsKey(this.Prev))
            {
                this.MergeSeq(this.HistoryMap[target], this.SentMap[this.Prev]);
                this.CheckEqual(this.HistoryMap[this.Prev], this.TempSeq);
            }

            this.ClearTempSeq();
        }

        private void UpdateServers()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Updating servers ...\n");
            this.Servers = (this.ReceivedEvent as eMonitorUpdateServers).uPayload;
        }

        private void IsSorted(List<int> seq)
        {
            for (int i = 0; i < seq.Count - 1; i++)
            {
                Assert(seq[i] < seq[i + 1], "Sequence is not sorted.");
            }
        }

        private void CheckLessThan(List<int> s1, List<int> s2)
        {
            Console.WriteLine("checking IsSorted in CheckLessThan");
            this.IsSorted(s1);
            Console.WriteLine("checking IsSorted in CheckLessThan again");
            this.IsSorted(s2);
        }

        private void ExtractSeqId(List<Tuple<int, MachineId, Tuple<int, int>>> seq)
        {
            this.ClearTempSeq();

            for (int i = seq.Count - 1; i >= 0; i--)
            {
                if (this.TempSeq.Count > 0)
                {
                    this.TempSeq.Insert(0, seq[i].Item1);
                }
                else
                {
                    this.TempSeq.Add(seq[i].Item1);
                }
            }

            Console.WriteLine("checking IsSorted in ExtractSeqId");
            this.IsSorted(this.TempSeq);
        }

        private void MergeSeq(List<int> s1, List<int> s2)
        {
            this.ClearTempSeq();
            Console.WriteLine("checking IsSorted in MergeSeq");
            this.IsSorted(s1);

            if (s1.Count == 0)
            {
                this.TempSeq = s2;
            }
            else if (s2.Count == 0)
            {
                this.TempSeq = s1;
            }
            else
            {
                for (int i = 0; i < s1.Count; i++)
                {
                    if (s1[i] < s2[0])
                    {
                        this.TempSeq.Add(s1[i]);
                    }
                }

                for (int i = 0; i < s2.Count; i++)
                {
                    this.TempSeq.Add(s2[i]);
                }
            }

            Console.WriteLine("checking IsSorted in MergeSeq again");
            this.IsSorted(this.TempSeq);
        }

        private void CheckEqual(List<int> s1, List<int> s2)
        {
            //for (int i = s1.Count - 1; i >= 0; i--)
            //{
            //    if (s2.Count > i)
            //    {
            //        Runtime.Assert(s1[i] == s2[i], "S1[{0}] and S2[{0}] are not equal.", i);
            //    }
            //}
        }

        private void GetNext(MachineId curr)
        {
            this.Next = null;

            for (int i = 1; i < this.Servers.Count; i++)
            {
                if (this.Servers[i - 1].Equals(curr))
                {
                    this.Next = this.Servers[i];
                }
            }
        }

        private void GetPrev(MachineId curr)
        {
            this.Prev = null;

            for (int i = 1; i < this.Servers.Count; i++)
            {
                if (this.Servers[i].Equals(curr))
                {
                    this.Prev = this.Servers[i - 1];
                }
            }
        }

        private void ClearTempSeq()
        {
            Assert(this.TempSeq.Count <= 6, "Temp sequence has more than 6 elements.");
            this.TempSeq.Clear();
            Assert(this.TempSeq.Count == 0, "Temp sequence is not cleared.");
        }

        private void Stop()
        {
            Console.WriteLine("[UpdatePropagationInvariantMonitor] Stopping ...\n");

            Raise(new Halt());
        }
        #endregion
    }
}
