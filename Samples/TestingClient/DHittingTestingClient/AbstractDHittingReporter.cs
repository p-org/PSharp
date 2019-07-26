
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingClientInterface.SimpleImplementation;
using System.Diagnostics;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace DHittingTestingClient
{
    public abstract class AbstractDHittingReporter : IMetricReporter
    {
        public int MaxDToCount;

        protected readonly DHittingUtils.DHittingSignature StepSignatureType;

        public DTupleTree DTupleTree;

        protected TimelyStatisticLogger<ulong[]> StatLogger;

        // Debug Statistics
        protected int DebugStatAddOrUpdateChildCalls;
        private HashSet<ulong> DebugStatUniqueSigs;

        public Stopwatch DebugStatStopWatch;

        public AbstractBaseProgramModelStrategy RecordingStrategy;

        public AbstractDHittingReporter(int maxDToCount, DHittingUtils.DHittingSignature stepSignatureType)
        {
            this.MaxDToCount = maxDToCount;
            this.StepSignatureType = stepSignatureType;

            this.DTupleTree = new DTupleTree(this.MaxDToCount);

            this.StatLogger = new TimelyStatisticLogger<ulong[]>();

            this.ResetLocalVariables();

            this.DebugStatAddOrUpdateChildCalls = 0;
            this.DebugStatUniqueSigs = new HashSet<ulong>();
            this.DebugStatStopWatch = new System.Diagnostics.Stopwatch();
        }

        protected abstract void ResetLocalVariables();

        protected abstract void EnumerateDTuples(List<IProgramStep> schedule);

        public void RecordIteration(ISchedulingStrategy strategy, bool bugFound)
        {
            this.RecordingStrategy = strategy as AbstractBaseProgramModelStrategy;

            List<IProgramStep> schedule = this.RecordingStrategy.GetSchedule();
            this.ComputeStepSignatures(schedule);
            this.EnumerateDTuples(schedule);

            ulong[] iterStats = new ulong[this.MaxDToCount];
            for (int i = 1; i <= this.MaxDToCount; i++)
            {
                iterStats[i - 1] = this.GetDTupleCount(i);
            }

            this.StatLogger.AddValue(iterStats);

            // reset
            this.ResetLocalVariables();

            // string s = this.ProgramModel.HAXGetProgramTreeString();
            // string dt = DTupleTreeNode.HAXGetDTupleTreeString(this.DTupleTreeRoot);
            // string dtr = this.DTupleTreeRoot.GetReport(this.MaxDToCount);
            // Console.WriteLine(this.GetProgramTrace());
            // PrintDTupleTree(this.DTupleTreeRoot, 0);

            this.RecordingStrategy = null;
        }

        

        private void ComputeStepSignatures(List<IProgramStep> schedule)
        {
            switch (this.StepSignatureType)
            {
                case DHittingUtils.DHittingSignature.TreeHash:
                    this.ComputeTreeHashStepSignatures(schedule);
                    break;
                case DHittingUtils.DHittingSignature.EventTypeIndex:
                    this.ComputeEventTypeIndexStepSignatures(schedule);
                    break;
                case DHittingUtils.DHittingSignature.EventHash:
                    this.ComputeEventHashStepSignatures(schedule);
                    break;
            }
        }
        // Specific StepSignature implementations
        private void ComputeTreeHashStepSignatures(List<IProgramStep> schedule)
        {
            Dictionary<ulong, ulong> machineIdRemap = new Dictionary<ulong, ulong>();
            machineIdRemap[DHittingUtils.TESTHARNESSMACHINEID] = DHittingUtils.TESTHARNESSMACHINEHASH;
            foreach (IProgramStep progStep in schedule)
            {
                progStep.Signature = new TreeHashStepSignature(progStep, machineIdRemap);
                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep && progStep.OpType == Microsoft.PSharp.TestingServices.Scheduling.AsyncOperationType.Create)
                {
                    machineIdRemap[progStep.TargetId] = (progStep.Signature as TreeHashStepSignature).Hash;
                }

                this.DebugStatUniqueSigs.Add((progStep.Signature as TreeHashStepSignature).Hash);
            }
        }

        private void ComputeEventHashStepSignatures(List<IProgramStep> schedule)
        {
            foreach (IProgramStep progStep in schedule)
            {
                progStep.Signature = new EventHashStepSignature(progStep);
                this.DebugStatUniqueSigs.Add((ulong)(progStep.Signature as EventHashStepSignature).Hash);
            }
        }

        // Note : This does Send events.
        private void ComputeEventTypeIndexStepSignatures(List<IProgramStep> schedule)
        {
            Dictionary<ulong, Type> srcIdToMachineType = this.RecordingStrategy.GetMachineIdToTypeMap();

            Dictionary<Tuple<ulong, string>, int> inboxEventIndexCounter = new Dictionary<Tuple<ulong, string>, int>();
            foreach (IProgramStep progStep in schedule)
            {
                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep && progStep.OpType == Microsoft.PSharp.TestingServices.Scheduling.AsyncOperationType.Send)
                {
                    // Tuple<ulong, string> ieicKey = new Tuple<ulong, string>(progStep.SrcId, progStep.EventInfo?.EventName ?? "NullEventInfo");
                    Tuple<ulong, string> ieicKey = Tuple.Create<ulong, string>(progStep.TargetId, progStep.EventInfo?.GetType().FullName ?? "NullEventInfo");

                    if (!inboxEventIndexCounter.ContainsKey(ieicKey))
                    {
                        inboxEventIndexCounter.Add(ieicKey, 0);
                    }

                    int currentIndex = inboxEventIndexCounter[ieicKey];
                    progStep.Signature = new EventTypeIndexStepSignature(progStep, srcIdToMachineType[progStep.TargetId].GetType(), currentIndex);

                    inboxEventIndexCounter[ieicKey] = currentIndex + 1;
                }
                else
                {
                    progStep.Signature = new EventTypeIndexStepSignature(progStep, srcIdToMachineType[progStep.SrcId].GetType(), -1);
                }
            }
        }

        public string GetReport()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"--- DHitting metrics - {this.ToString()} ---");

            foreach (Tuple<int, ulong[]> stat in this.StatLogger)
            {
                string s = string.Join("\t", stat.Item2);
                sb.AppendLine($"{stat.Item1}\t:\t{s}");
            }

            Tuple<int, ulong[]> finalStat = this.StatLogger.GetFinalValue();
            sb.AppendLine("-\t:\t-\t-\t-");
            string s2 = string.Join("\t", finalStat.Item2);
            sb.AppendLine($"{finalStat.Item1}\t:\t{s2}");

            long msElapsed = this.DebugStatStopWatch.ElapsedMilliseconds;

            sb.AppendLine($"SW.TimeToEnumerateMs={msElapsed} ; AddUpdateCalls={this.DebugStatAddOrUpdateChildCalls}; perAddUpdateCallMicros={msElapsed / ((float)this.DebugStatAddOrUpdateChildCalls / 1000)}");

            return sb.ToString();
        }


        public ulong GetDTupleCount(int d)
        {
            return this.DTupleTree.GetDTupleCount(d);
        }

        public override string ToString()
        {
            return $"{this.GetType().Name}[{this.StepSignatureType}]";
        }


    }
}
