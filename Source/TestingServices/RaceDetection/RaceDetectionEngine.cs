// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.RaceDetection.InstrumentationState;
using Microsoft.PSharp.TestingServices.RaceDetection.Util;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.PSharp.TestingServices.RaceDetection
{
    using InstrMachineState = InstrumentationState.MachineState;

    internal class RaceDetectionEngine : IRegisterRuntimeOperation
    {
        /// <summary>
        /// The machine shadow state. M[mId] will get us the instrumentation
        /// state for a machine with id mId.
        /// </summary>
        private Dictionary<ulong, InstrMachineState> MS;

        /// <summary>
        /// The variable shadow state. V[(objHandle, offset)] will get us the instrumentation
        /// state for a read/write to objHandle at offset.
        /// </summary>
        private Dictionary<Tuple<UIntPtr, UIntPtr>, VarState> VS;

        /// <summary>
        /// An auxiliary data structure to help enforce the "deq-happens-after-enq" rule
        /// At a deq, look up the vector clock snapshot captured at the corresponding enqueue
        /// as ES[seq#], where the enqueue has global sequence number seq#
        /// We use the seq# to disambiguate multiple posts with the same source, target and event object
        /// since in P#, the reuse of events is permitted.
        /// </summary>
        private Dictionary<ulong, VectorClock> ES;

        /// <summary>
        /// Track the names of machines. Used when we report races
        /// </summary>
        private Dictionary<ulong, string> DescriptiveName;

        /// <summary>
        /// A logger and configuration from the runtime to report races
        /// found (and possibly debug logs).
        /// </summary>
        private ILogger Log;

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Config;

        private TestReport TestReport;

        /// <summary>
        /// We need a reference to the runtime to query it for the currently
        /// executing machine's Id at read/write operations
        /// </summary>
        private BugFindingRuntime Runtime;

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
        /// Constructor.
        /// </summary>
        public RaceDetectionEngine(Configuration config, ILogger logger, TestReport testReport)
        {
            MS = new Dictionary<ulong, InstrMachineState>();
            VS = new Dictionary<Tuple<UIntPtr, UIntPtr>, VarState>();
            ES = new Dictionary<ulong, VectorClock>();
            DescriptiveName = new Dictionary<ulong, string>();
            InAction = new Dictionary<ulong, bool>();
            InMonitor = -1;
            this.Log = logger;
            this.Config = config;
            this.TestReport = testReport;
            ResetCounters();
        }

        public Dictionary<ulong, bool> InAction { get; set; }

        public long InMonitor { get; set; }

        public bool TryGetCurrentMachineId(out ulong machineId)
        {
            var mid = this.Runtime.GetCurrentMachineId();
            if (mid == null)
            {
                machineId = 0;
                return false;
            }
            machineId = mid.Value;
            return true;
        }

        public void SetRuntime(PSharpRuntime runtime)
        {
            runtime.Assert((runtime as BugFindingRuntime) != null,
                "Requires passed runtime to support method GetCurrentMachineId");
            this.Runtime = runtime as BugFindingRuntime;
        }

        public void RegisterCreateMachine(MachineId source, MachineId target)
        {
            LogCreate(source, target);
            CreateCount++;

            // The id of the created machine should not conflict with an id seen earlier
            Runtime.Assert(MS.ContainsKey(target.Value) == false, $"New ID {target} conflicts with an already existing id");

            DescriptiveName[target.Value] = target.ToString();

            // In case the runtime creates a machine, simply create a machine state for it,
            // with a fresh VC where the appropriate component is incremented.
            // no hb rule needs to be triggered
            if (source == null)
            {
                var newState = new InstrMachineState(target.Value, this.Log, Config.EnableRaceDetectorLogging);
                MS[target.Value] = newState;
                return;
            }

            DescriptiveName[source.Value] = source.ToString();

            var sourceMachineState = GetCurrentState(source);
            var targetState = new InstrMachineState(target.Value, this.Log, Config.EnableRaceDetectorLogging);
            targetState.JoinEpochAndVC(sourceMachineState.VC);
            MS[target.Value] = targetState;
            sourceMachineState.IncrementEpochAndVC();
        }

        public void RegisterDequeue(MachineId source, MachineId target, Event e, ulong sequenceNumber)
        {
            LogDequeue(source, target, e, sequenceNumber);
            DequeueCount++;

            var currentState = GetCurrentState(target);

            // We saw a deq without a post.
            // This message came from a client outside the PSharp runtime, so
            // we can't infer any hb relation
            if (ES.ContainsKey(sequenceNumber) == false)
            {
                currentState.IncrementEpochAndVC();
                return;
            }

            currentState.JoinThenIncrement(ES[sequenceNumber]);
        }

        public void RegisterEnqueue(MachineId source, MachineId target, Event e, ulong sequenceNumber)
        {
            LogEnqueue(source, target, e, sequenceNumber);
            EnqueueCount++;

            var currentState = GetCurrentState(source);
            ES[sequenceNumber] = new VectorClock(currentState.VC);
            currentState.IncrementEpochAndVC();
        }

        public void RegisterRead(ulong source, string sourceLocation, UIntPtr location, UIntPtr objHandle, UIntPtr offset, bool isVolatile)
        {
            LogRead(sourceLocation, source, objHandle, offset);
            ReadCount++;

            var key = new Tuple<UIntPtr, UIntPtr>(objHandle, offset);

            // For Raise actions and init actions, we might not have seen a dequeue
            // of the action yet, so source \in MS is not guaranteed
            if (!MS.ContainsKey(source))
            {
                // WriteToLog("Saw a read in an action without a corresponding deq");
                MS[source] = new InstrMachineState(source, this.Log, Config.EnableRaceDetectorLogging);
            }

            // Implementation of the FastTrack rules for read operations
            var machineState = MS[source];
            var currentEpoch = machineState.Epoch;
            if (VS.ContainsKey(key))
            {
                var varState = VS[key];
                varState.InMonitorRead[(long)source] = this.InMonitor;
                if (Config.EnableReadWriteTracing)
                {
                    varState.LastReadLocation[(long)source] = sourceLocation;
                }

                if (varState.ReadEpoch == currentEpoch)
                {
                    // Same-epoch read
                    return;
                }

                VectorClock mVC = machineState.VC;
                long readEpoch = varState.ReadEpoch;
                long writeEpoch = varState.WriteEpoch;
                long writeMId = Epoch.MId(writeEpoch);
                long currentMId = (long)source;

                // The lastest write was from a diff machine, and no HB
                if (writeMId != currentMId && !Epoch.Leq(writeEpoch, mVC.GetComponent(writeMId)) &&
                    !InSameMonitor(varState.InMonitorWrite, this.InMonitor))
                {
                    // Write/Read race
                    ReportRace(RaceDiagnostic.WriteRead, varState.lastWriteLocation, writeMId, sourceLocation, currentMId, objHandle, offset);
                    return;
                }

                if (readEpoch == Epoch.ReadShared)
                {
                    Runtime.Assert((long)currentMId == Epoch.MId(currentEpoch), "Inconsistent Epoch");
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
                        if (varState.VC == null)
                        {
                            varState.VC = new VectorClock(Math.Max(rMId, currentMId));
                        }
                        varState.VC.SetComponent(rMId, readEpoch);
                        varState.VC.SetComponent(currentMId, currentEpoch);
                        varState.ReadEpoch = Epoch.ReadShared;
                    }
                }
            }
            else // The first read from this variable
            {
                var currentState = new VarState(false, currentEpoch, Config.EnableReadWriteTracing, this.InMonitor);
                currentState.InMonitorRead[(long)source] = this.InMonitor;
                if (Config.EnableReadWriteTracing)
                {
                    currentState.LastReadLocation[(long)source] = sourceLocation;
                }
                VS[key] = currentState;
            }
        }

        public void RegisterWrite(ulong source, string sourceLocation,
            UIntPtr location, UIntPtr objHandle, UIntPtr offset, bool isVolatile)
        {
            LogWrite(sourceLocation, source, objHandle, offset);
            WriteCount++;

            var key = new Tuple<UIntPtr, UIntPtr>(objHandle, offset);

            // For Raise actions and init actions, we might not have seen a dequeue
            // of the action yet, so source \in MS is not guaranteed
            if (!MS.ContainsKey(source))
            {
                // WriteToLog("Saw a write in an action without a corresponding deq");
                var newState = new InstrMachineState(source, this.Log, Config.EnableRaceDetectorLogging);
                MS[source] = newState;
            }

            // Implementation of the FastTrack rules for write operations
            var machineState = MS[source];
            var currentEpoch = machineState.Epoch;
            var currentMId = Epoch.MId(machineState.Epoch);
            var currentVC = machineState.VC;

            Runtime.Assert(currentMId == (long)source, "Inconsistent Epoch");

            if (VS.ContainsKey(key))
            {
                var varState = VS[key];
                var writeEpoch = varState.WriteEpoch;
                var readEpoch = varState.ReadEpoch;
                var writeMId = Epoch.MId(writeEpoch);

                if (writeEpoch == currentEpoch)
                {
                    // Same-epoch write
                    return;
                }

                if (writeMId != currentMId && !Epoch.Leq(writeEpoch, currentVC.GetComponent(writeMId)) &&
                    !InSameMonitor(varState.InMonitorWrite, this.InMonitor))
                {
                    ReportRace(RaceDiagnostic.WriteWrite, varState.lastWriteLocation, writeMId, sourceLocation, currentMId, objHandle, offset);
                }

                varState.InMonitorWrite = this.InMonitor;
                if (Config.EnableReadWriteTracing)
                {
                    varState.lastWriteLocation = sourceLocation;
                }

                if (readEpoch != Epoch.ReadShared)
                {
                    var readMId = Epoch.MId(readEpoch);
                    if (readMId != currentMId && !Epoch.Leq(readEpoch, currentVC.GetComponent(readMId)) &&
                        !InSameMonitor(varState.InMonitorRead[readMId], this.InMonitor))
                    {
                        // Read-Write Race
                        string firstLocation = Config.EnableReadWriteTracing ? varState.LastReadLocation[readMId] : "";
                        ReportRace(RaceDiagnostic.ReadWrite, firstLocation, readMId, sourceLocation, currentMId, objHandle, offset);
                    }
                }
                else
                {
                    if (varState.VC.AnyGt(currentVC))
                    {
                        // SharedRead-Write Race
                        ReportReadSharedWriteRace(sourceLocation, currentMId, currentVC, varState, objHandle, offset);
                    }
                    else
                    {
                        // Note: the FastTrack implementation seems not to do this
                        varState.ReadEpoch = Epoch.Zero;
                    }
                }

                varState.WriteEpoch = currentEpoch;
            }
            else
            {
                VS[key] = new VarState(true, currentEpoch, Config.EnableReadWriteTracing, this.InMonitor);
                if (Config.EnableReadWriteTracing)
                {
                    VS[key].lastWriteLocation = sourceLocation;
                }
            }
        }

        public void ClearAll()
        {
            this.MS.Clear();
            this.ES.Clear();
            this.VS.Clear();
            this.InAction.Clear();
            this.Runtime.Logger.WriteLine($"Iteration stats " +
                $"Enq:{EnqueueCount} Deq:{DequeueCount} Create:{CreateCount} Read:{ReadCount} Write:{WriteCount}");
            ResetCounters();
        }

        private enum RaceDiagnostic
        { ReadWrite, WriteWrite, WriteRead, WriteReadShared };

        private void ResetCounters()
        {
            EnqueueCount = 0;
            DequeueCount = 0;
            ReadCount = 0;
            WriteCount = 0;
            CreateCount = 0;
        }

        private void ReportRace(String diagnostic, string first, long fId, string second, long sId)
        {
            Config.RaceFound = true;
            var nL = Environment.NewLine;
            var firstId = DescriptiveName[(ulong)fId];
            var secondId = DescriptiveName[(ulong)sId];
            //Removing diagnostic from the report string
            string report = $"****RACE:****{nL}\t\t {first}:{firstId}{nL}\t\t {second}:{secondId}";
            Log.WriteLine(report);
            this.TestReport.BugReports.Add(report);
        }

        private void ReportRace(RaceDiagnostic diagnostic, string firstLocation, long first, string secondLocation, long second,
            UIntPtr objHandle, UIntPtr offset)
        {
            switch (diagnostic)
            {
                case RaceDiagnostic.WriteRead:
                    string writeInfo = "Write by: ";
                    string readInfo = "Read by:";
                    if (Config.EnableReadWriteTracing)
                    {
                        writeInfo = String.Format("Write ({0}) by", firstLocation);
                        readInfo = String.Format("Read ({0}) by", secondLocation);
                    }
                    ReportRace($"Write/Read[{objHandle}/{offset}]", writeInfo, first, readInfo, second);
                    break;

                case RaceDiagnostic.WriteWrite:
                    string firstWriteInfo = "Write by: ";
                    string secondWriteInfo = "Write by:";
                    if (Config.EnableReadWriteTracing)
                    {
                        firstWriteInfo = String.Format("Write ({0}) by", firstLocation);
                        secondWriteInfo = String.Format("Write ({0}) by", secondLocation);
                    }
                    ReportRace($"Write/Write[{objHandle}/{offset}]", firstWriteInfo, first, secondWriteInfo, second);
                    break;

                case RaceDiagnostic.ReadWrite:
                    readInfo = "Read by: ";
                    writeInfo = "Write by:";
                    if (Config.EnableReadWriteTracing)
                    {
                        readInfo = String.Format("Read ({0}) by", firstLocation);
                        writeInfo = String.Format("Write ({0}) by", secondLocation);
                    }
                    ReportRace($"Read/Write[{objHandle}/{offset}]", readInfo, first, writeInfo, second);
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
            if (Config.EnableReadWriteTracing)
            {
                writeInfo = String.Format("Write ({0}) by", sourceLocation);
            }

            for (int previousReader = varState.VC.NextGT(currentVC, 0); previousReader > -1;
                previousReader = varState.VC.NextGT(currentVC, previousReader + 1))
            {
                // Read-Shared - Write race between previousReader and currentMId
                if (Config.EnableReadWriteTracing)
                {
                    readInfo = String.Format("Shared Read ({0}) by", varState.LastReadLocation[(long)previousReader]);
                }
                if (!InSameMonitor(varState.InMonitorRead[(long)previousReader], this.InMonitor))
                {
                    ReportRace($"Read-Shared/Write[{objHandle}/{offset}]", readInfo, previousReader, writeInfo, currentMId);
                }
            }
        }

        private bool InSameMonitor(long firstMonitor, long secondMonitor)
        {
            // both accesses are outside monitors, therefore not in the same monitor
            if (firstMonitor == -1 && secondMonitor == -1)
            {
                return false;
            }

            return firstMonitor == secondMonitor;
        }

        private InstrMachineState GetCurrentState(MachineId machineId)
        {
            if (MS.ContainsKey(machineId.Value))
            {
                return MS[machineId.Value];
            }

            // WriteToLog("Saw first operation for " + machineId);
            var newState = new InstrMachineState(machineId.Value, this.Log, Config.EnableRaceDetectorLogging);
            MS[machineId.Value] = newState;
            return newState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogCreate(MachineId source, MachineId target)
        {
            if (Config.EnableRaceDetectorLogging)
            {
                Log.WriteLine($"<RaceLog> Create({source}, {target})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogDequeue(MachineId source, MachineId target, Event e, ulong sequenceNumber)
        {
            if (Config.EnableRaceDetectorLogging)
            {
                Log.WriteLine($"<RaceLog> Deq({source}, {target}, {e}, {sequenceNumber})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogEnqueue(MachineId source, MachineId target, Event e, ulong sequenceNumber)
        {
            if (Config.EnableRaceDetectorLogging)
            {
                Log.WriteLine($"<RaceLog> Enq({source}, {target}, {e}, {sequenceNumber})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogRead(string sourceLocation, ulong source, UIntPtr objHandle, UIntPtr offset)
        {
            if (Config.EnableRaceDetectorLogging)
            {
                Log.WriteLine($"<RaceLog> Read({sourceLocation}, {source}, {objHandle}, {offset})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogWrite(string sourceLocation, ulong source, UIntPtr objHandle, UIntPtr offset)
        {
            if (Config.EnableRaceDetectorLogging)
            {
                Log.WriteLine($"<RaceLog> Write({sourceLocation}, {source}, {objHandle}, {offset})");
            }
        }
    }
}