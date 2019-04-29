// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.RaceDetection.InstrumentationState;
using Microsoft.PSharp.TestingServices.RaceDetection.Util;
using Microsoft.PSharp.TestingServices.Runtime;

using InstrMachineState = Microsoft.PSharp.TestingServices.RaceDetection.InstrumentationState.MachineState;

namespace Microsoft.PSharp.TestingServices.RaceDetection
{
    internal class RaceDetectionEngine : IRegisterRuntimeOperation
    {
        /// <summary>
        /// The machine shadow state. M[mId] will get us the instrumentation
        /// state for a machine with id mId.
        /// </summary>
        private readonly Dictionary<ulong, InstrMachineState> MachineState;

        /// <summary>
        /// The variable shadow state. V[(objHandle, offset)] will get us the instrumentation
        /// state for a read/write to objHandle at offset.
        /// </summary>
        private readonly Dictionary<Tuple<UIntPtr, UIntPtr>, VarState> VarState;

        /// <summary>
        /// An auxiliary data structure to help enforce the "deq-happens-after-enq" rule
        /// At a deq, look up the vector clock snapshot captured at the corresponding enqueue
        /// as EventState[seq#], where the enqueue has global sequence number seq#
        /// We use the seq# to disambiguate multiple posts with the same source, target and event object
        /// since in P#, the reuse of events is permitted.
        /// </summary>
        private readonly Dictionary<ulong, VectorClock> EventState;

        /// <summary>
        /// Track the names of machines. Used when we report races
        /// </summary>
        private readonly Dictionary<ulong, string> DescriptiveName;

        /// <summary>
        /// A logger and configuration from the runtime to report races
        /// found (and possibly debug logs).
        /// </summary>
        private readonly ILogger Log;

        /// <summary>
        /// Configuration.
        /// </summary>
        private readonly Configuration Config;

        /// <summary>
        /// The test report.
        /// </summary>
        private readonly TestReport TestReport;

        /// <summary>
        /// We need a reference to the runtime to query it for the currently
        /// executing machine's Id at read/write operations.
        /// </summary>
        private SystematicTestingRuntime Runtime;

        /// <summary>
        /// Counter to track the number of enqueue operations.
        /// </summary>
        private ulong EnqueueCount;

        /// <summary>
        /// Counter to track the number of dequeue operations.
        /// </summary>
        private ulong DequeueCount;

        /// <summary>
        /// Counter to track the number of read operations.
        /// </summary>
        private ulong ReadCount;

        /// <summary>
        /// Counter to track the number of write operations.
        /// </summary>
        private ulong WriteCount;

        /// <summary>
        /// Counter to track the number of create machine operations.
        /// </summary>
        private ulong CreateCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="RaceDetectionEngine"/> class.
        /// </summary>
        public RaceDetectionEngine(Configuration config, ILogger logger, TestReport testReport)
        {
            this.MachineState = new Dictionary<ulong, InstrMachineState>();
            this.VarState = new Dictionary<Tuple<UIntPtr, UIntPtr>, VarState>();
            this.EventState = new Dictionary<ulong, VectorClock>();
            this.DescriptiveName = new Dictionary<ulong, string>();
            this.InAction = new Dictionary<ulong, bool>();
            this.InMonitor = -1;
            this.Log = logger;
            this.Config = config;
            this.TestReport = testReport;
            this.ResetCounters();
        }

        public Dictionary<ulong, bool> InAction { get; set; }

        public long InMonitor { get; set; }

        public bool TryGetCurrentMachineId(out ulong machineId)
        {
            var mid = this.Runtime.GetCurrentMachineId();
            if (mid is null)
            {
                machineId = 0;
                return false;
            }

            machineId = mid.Value;
            return true;
        }

        /// <summary>
        /// Registers the testing runtime.
        /// </summary>
        public void RegisterRuntime(IMachineRuntime runtime)
        {
            runtime.Assert((runtime as SystematicTestingRuntime) != null,
                "Requires passed runtime to support method GetCurrentMachineId");
            this.Runtime = runtime as SystematicTestingRuntime;
        }

        public void RegisterCreateMachine(MachineId source, MachineId target)
        {
            this.LogCreate(source, target);
            this.CreateCount++;

            // The id of the created machine should not conflict with an id seen earlier.
            this.Runtime.Assert(this.MachineState.ContainsKey(target.Value) == false, $"New ID {target} conflicts with an already existing id");

            this.DescriptiveName[target.Value] = target.ToString();

            // In case the runtime creates a machine, simply create a machine state
            // for it, with a fresh VC where the appropriate component is incremented.
            // No hb rule needs to be triggered.
            if (source is null)
            {
                var newState = new InstrMachineState(target.Value, this.Log, this.Config.EnableRaceDetectorLogging);
                this.MachineState[target.Value] = newState;
                return;
            }

            this.DescriptiveName[source.Value] = source.ToString();

            var sourceMachineState = this.GetCurrentState(source);
            var targetState = new InstrMachineState(target.Value, this.Log, this.Config.EnableRaceDetectorLogging);
            targetState.JoinEpochAndVC(sourceMachineState.VC);
            this.MachineState[target.Value] = targetState;
            sourceMachineState.IncrementEpochAndVC();
        }

        public void RegisterDequeue(MachineId source, MachineId target, Event e, ulong sequenceNumber)
        {
            this.LogDequeue(source, target, e, sequenceNumber);
            this.DequeueCount++;

            var currentState = this.GetCurrentState(target);

            // We saw a deq without a post. This message came from a client outside
            // the runtime, so we can't infer any hb relation.
            if (this.EventState.ContainsKey(sequenceNumber) == false)
            {
                currentState.IncrementEpochAndVC();
                return;
            }

            currentState.JoinThenIncrement(this.EventState[sequenceNumber]);
        }

        public void RegisterEnqueue(MachineId source, MachineId target, Event e, ulong sequenceNumber)
        {
            this.LogEnqueue(source, target, e, sequenceNumber);
            this.EnqueueCount++;

            var currentState = this.GetCurrentState(source);
            this.EventState[sequenceNumber] = new VectorClock(currentState.VC);
            currentState.IncrementEpochAndVC();
        }

        public void RegisterRead(ulong source, string sourceLocation, UIntPtr location, UIntPtr objHandle, UIntPtr offset, bool isVolatile)
        {
            this.LogRead(sourceLocation, source, objHandle, offset);
            this.ReadCount++;

            var key = new Tuple<UIntPtr, UIntPtr>(objHandle, offset);

            // For Raise actions and init actions, we might not have seen a dequeue
            // of the action yet, so source \in MachineState is not guaranteed.
            if (!this.MachineState.ContainsKey(source))
            {
                // WriteToLog("Saw a read in an action without a corresponding deq");
                this.MachineState[source] = new InstrMachineState(source, this.Log, this.Config.EnableRaceDetectorLogging);
            }

            // Implementation of the FastTrack rules for read operations.
            var machineState = this.MachineState[source];
            var currentEpoch = machineState.Epoch;
            if (this.VarState.ContainsKey(key))
            {
                var varState = this.VarState[key];
                varState.InMonitorRead[(long)source] = this.InMonitor;
                if (this.Config.EnableReadWriteTracing)
                {
                    varState.LastReadLocation[(long)source] = sourceLocation;
                }

                if (varState.ReadEpoch == currentEpoch)
                {
                    // Same-epoch read.
                    return;
                }

                VectorClock mVC = machineState.VC;
                long readEpoch = varState.ReadEpoch;
                long writeEpoch = varState.WriteEpoch;
                long writeMId = Epoch.MId(writeEpoch);
                long currentMId = (long)source;

                // The lastest write was from a diff machine, and no HB.
                if (writeMId != currentMId && !Epoch.Leq(writeEpoch, mVC.GetComponent(writeMId)) &&
                    !InSameMonitor(varState.InMonitorWrite, this.InMonitor))
                {
                    // Write/read race.
                    this.ReportRace(RaceDiagnostic.WriteRead, varState.LastWriteLocation, writeMId, sourceLocation, currentMId, objHandle, offset);
                    return;
                }

                if (readEpoch == Epoch.ReadShared)
                {
                    this.Runtime.Assert(currentMId == Epoch.MId(currentEpoch), "Inconsistent Epoch");
                    varState.VC.SetComponent(currentMId, currentEpoch);
                }
                else
                {
                    long rMId = Epoch.MId(readEpoch);
                    if (currentMId == rMId || Epoch.Leq(readEpoch, mVC.GetComponent(rMId)))
                    {
                        varState.ReadEpoch = currentEpoch;
                    }
                    else
                    {
                        if (varState.VC is null)
                        {
                            varState.VC = new VectorClock(Math.Max(rMId, currentMId));
                        }

                        varState.VC.SetComponent(rMId, readEpoch);
                        varState.VC.SetComponent(currentMId, currentEpoch);
                        varState.ReadEpoch = Epoch.ReadShared;
                    }
                }
            }
            else
            {
                // The first read from this variable.
                var currentState = new VarState(false, currentEpoch, this.Config.EnableReadWriteTracing, this.InMonitor);
                currentState.InMonitorRead[(long)source] = this.InMonitor;
                if (this.Config.EnableReadWriteTracing)
                {
                    currentState.LastReadLocation[(long)source] = sourceLocation;
                }

                this.VarState[key] = currentState;
            }
        }

        public void RegisterWrite(ulong source, string sourceLocation,
            UIntPtr location, UIntPtr objHandle, UIntPtr offset, bool isVolatile)
        {
            this.LogWrite(sourceLocation, source, objHandle, offset);
            this.WriteCount++;

            var key = new Tuple<UIntPtr, UIntPtr>(objHandle, offset);

            // For Raise actions and init actions, we might not have seen a dequeue
            // of the action yet, so source \in MachineState is not guaranteed.
            if (!this.MachineState.ContainsKey(source))
            {
                // WriteToLog("Saw a write in an action without a corresponding deq");
                var newState = new InstrMachineState(source, this.Log, this.Config.EnableRaceDetectorLogging);
                this.MachineState[source] = newState;
            }

            // Implementation of the FastTrack rules for write operations.
            var machineState = this.MachineState[source];
            var currentEpoch = machineState.Epoch;
            var currentMId = Epoch.MId(machineState.Epoch);
            var currentVC = machineState.VC;

            this.Runtime.Assert(currentMId == (long)source, "Inconsistent Epoch");

            if (this.VarState.ContainsKey(key))
            {
                var varState = this.VarState[key];
                var writeEpoch = varState.WriteEpoch;
                var readEpoch = varState.ReadEpoch;
                var writeMId = Epoch.MId(writeEpoch);

                if (writeEpoch == currentEpoch)
                {
                    // Same-epoch write.
                    return;
                }

                if (writeMId != currentMId && !Epoch.Leq(writeEpoch, currentVC.GetComponent(writeMId)) &&
                    !InSameMonitor(varState.InMonitorWrite, this.InMonitor))
                {
                    this.ReportRace(RaceDiagnostic.WriteWrite, varState.LastWriteLocation, writeMId, sourceLocation, currentMId, objHandle, offset);
                }

                varState.InMonitorWrite = this.InMonitor;
                if (this.Config.EnableReadWriteTracing)
                {
                    varState.LastWriteLocation = sourceLocation;
                }

                if (readEpoch != Epoch.ReadShared)
                {
                    var readMId = Epoch.MId(readEpoch);
                    if (readMId != currentMId && !Epoch.Leq(readEpoch, currentVC.GetComponent(readMId)) &&
                        !InSameMonitor(varState.InMonitorRead[readMId], this.InMonitor))
                    {
                        // Read-write Race.
                        string firstLocation = this.Config.EnableReadWriteTracing ? varState.LastReadLocation[readMId] : string.Empty;
                        this.ReportRace(RaceDiagnostic.ReadWrite, firstLocation, readMId, sourceLocation, currentMId, objHandle, offset);
                    }
                }
                else
                {
                    if (varState.VC.AnyGt(currentVC))
                    {
                        // SharedRead-write Race.
                        this.ReportReadSharedWriteRace(sourceLocation, currentMId, currentVC, varState, objHandle, offset);
                    }
                    else
                    {
                        // Note: the FastTrack implementation seems not to do this.
                        varState.ReadEpoch = Epoch.Zero;
                    }
                }

                varState.WriteEpoch = currentEpoch;
            }
            else
            {
                this.VarState[key] = new VarState(true, currentEpoch, this.Config.EnableReadWriteTracing, this.InMonitor);
                if (this.Config.EnableReadWriteTracing)
                {
                    this.VarState[key].LastWriteLocation = sourceLocation;
                }
            }
        }

        public void ClearAll()
        {
            this.MachineState.Clear();
            this.EventState.Clear();
            this.VarState.Clear();
            this.InAction.Clear();
            this.Runtime.Logger.WriteLine($"Iteration stats " +
                $"Enq:{this.EnqueueCount} Deq:{this.DequeueCount} Create:{this.CreateCount} Read:{this.ReadCount} Write:{this.WriteCount}");
            this.ResetCounters();
        }

        private enum RaceDiagnostic
        {
            ReadWrite,
            WriteWrite,
            WriteRead,
            WriteReadShared
        }

        private void ResetCounters()
        {
            this.EnqueueCount = 0;
            this.DequeueCount = 0;
            this.ReadCount = 0;
            this.WriteCount = 0;
            this.CreateCount = 0;
        }

#pragma warning disable CA1801
        private void ReportRace(string diagnostic, string first, long fId, string second, long sId)
        {
            this.Config.RaceFound = true;
            var nL = Environment.NewLine;
            var firstId = this.DescriptiveName[(ulong)fId];
            var secondId = this.DescriptiveName[(ulong)sId];

            // Removing diagnostic from the report string.
            string report = $"****RACE:****{nL}\t\t {first}:{firstId}{nL}\t\t {second}:{secondId}";
            this.Log.WriteLine(report);
            this.TestReport.BugReports.Add(report);
        }
#pragma warning restore CA1801

        private void ReportRace(RaceDiagnostic diagnostic, string firstLocation, long first, string secondLocation, long second,
            UIntPtr objHandle, UIntPtr offset)
        {
            switch (diagnostic)
            {
                case RaceDiagnostic.WriteRead:
                    string writeInfo = "Write by: ";
                    string readInfo = "Read by:";
                    if (this.Config.EnableReadWriteTracing)
                    {
                        writeInfo = string.Format("Write ({0}) by", firstLocation);
                        readInfo = string.Format("Read ({0}) by", secondLocation);
                    }

                    this.ReportRace($"Write/Read[{objHandle}/{offset}]", writeInfo, first, readInfo, second);
                    break;

                case RaceDiagnostic.WriteWrite:
                    string firstWriteInfo = "Write by: ";
                    string secondWriteInfo = "Write by:";
                    if (this.Config.EnableReadWriteTracing)
                    {
                        firstWriteInfo = string.Format("Write ({0}) by", firstLocation);
                        secondWriteInfo = string.Format("Write ({0}) by", secondLocation);
                    }

                    this.ReportRace($"Write/Write[{objHandle}/{offset}]", firstWriteInfo, first, secondWriteInfo, second);
                    break;

                case RaceDiagnostic.ReadWrite:
                    readInfo = "Read by: ";
                    writeInfo = "Write by:";
                    if (this.Config.EnableReadWriteTracing)
                    {
                        readInfo = string.Format("Read ({0}) by", firstLocation);
                        writeInfo = string.Format("Write ({0}) by", secondLocation);
                    }

                    this.ReportRace($"Read/Write[{objHandle}/{offset}]", readInfo, first, writeInfo, second);
                    break;

                default:
                    break;
            }
        }

        private void ReportReadSharedWriteRace(string sourceLocation, long currentMId, VectorClock currentVC, VarState varState,
            UIntPtr objHandle, UIntPtr offset)
        {
            string writeInfo = "Write by:";
            string readInfo = "Shared Read by: ";
            if (this.Config.EnableReadWriteTracing)
            {
                writeInfo = string.Format("Write ({0}) by", sourceLocation);
            }

            for (int previousReader = varState.VC.NextGT(currentVC, 0); previousReader > -1;
                previousReader = varState.VC.NextGT(currentVC, previousReader + 1))
            {
                // Read-Shared - Write race between previousReader and currentMId.
                if (this.Config.EnableReadWriteTracing)
                {
                    readInfo = string.Format("Shared Read ({0}) by", varState.LastReadLocation[previousReader]);
                }

                if (!InSameMonitor(varState.InMonitorRead[previousReader], this.InMonitor))
                {
                    this.ReportRace($"Read-Shared/Write[{objHandle}/{offset}]", readInfo, previousReader, writeInfo, currentMId);
                }
            }
        }

        private static bool InSameMonitor(long firstMonitor, long secondMonitor)
        {
            // Both accesses are outside monitors, therefore not in the same monitor.
            if (firstMonitor == -1 && secondMonitor == -1)
            {
                return false;
            }

            return firstMonitor == secondMonitor;
        }

        private InstrMachineState GetCurrentState(MachineId machineId)
        {
            if (this.MachineState.ContainsKey(machineId.Value))
            {
                return this.MachineState[machineId.Value];
            }

            // WriteToLog("Saw first operation for " + machineId);
            var newState = new InstrMachineState(machineId.Value, this.Log, this.Config.EnableRaceDetectorLogging);
            this.MachineState[machineId.Value] = newState;
            return newState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogCreate(MachineId source, MachineId target)
        {
            if (this.Config.EnableRaceDetectorLogging)
            {
                this.Log.WriteLine($"<RaceLog> Create({source}, {target})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogDequeue(MachineId source, MachineId target, Event e, ulong sequenceNumber)
        {
            if (this.Config.EnableRaceDetectorLogging)
            {
                this.Log.WriteLine($"<RaceLog> Deq({source}, {target}, {e}, {sequenceNumber})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogEnqueue(MachineId source, MachineId target, Event e, ulong sequenceNumber)
        {
            if (this.Config.EnableRaceDetectorLogging)
            {
                this.Log.WriteLine($"<RaceLog> Enq({source}, {target}, {e}, {sequenceNumber})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogRead(string sourceLocation, ulong source, UIntPtr objHandle, UIntPtr offset)
        {
            if (this.Config.EnableRaceDetectorLogging)
            {
                this.Log.WriteLine($"<RaceLog> Read({sourceLocation}, {source}, {objHandle}, {offset})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogWrite(string sourceLocation, ulong source, UIntPtr objHandle, UIntPtr offset)
        {
            if (this.Config.EnableRaceDetectorLogging)
            {
                this.Log.WriteLine($"<RaceLog> Write({sourceLocation}, {source}, {objHandle}, {offset})");
            }
        }
    }
}
