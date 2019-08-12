// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    /// <summary>
    /// Tells us what sort of step an ProgramStep represents.
    /// </summary>
    public enum ProgramStepType
    {
        /// <summary>
        /// Reserved for special steps like the start (or a skip maybe?)
        /// </summary>
        SpecialProgramStepType,

        /// <summary>
        /// Your typical ISchedulable step. Those decided by GetNext
        /// </summary>
        SchedulableStep,

        /// <summary>
        /// A non-deterministic boolean choice. Those decided by GetNextBooleanChoice
        /// </summary>
        NonDetBoolStep,

        /// <summary>
        /// A non-deterministic boolean choice. Those decided by GetNextIntegerChoice
        /// </summary>
        NonDetIntStep
    }

    /// <summary>
    /// One step in the program.
    /// Should capture all the information needed for Program-aware scheduling strategies.
    /// </summary>
    public class ProgramStep
    {
        /// <summary>
        /// Tells what type this step is
        /// </summary>
        public readonly ProgramStepType ProgramStepType;

        // Main info

        /// <summary>
        /// If SchedulableStep, tells what kind of operationo was performed
        /// </summary>
        public readonly AsyncOperationType OpType;

        /// <summary>
        /// The MachineId.Value of the machine performing the step
        /// </summary>
        public readonly ulong SrcId;

        /// <summary>
        /// For Send/Create the MachineId.Value of the machine receiving/created.
        /// </summary>
        public readonly ulong TargetId;

        /// <summary>
        /// For Send/Receive/Create(?) the EventInfo of the event being sent.
        /// </summary>
        public ProgramStepEventInfo EventInfo { get; }

        /// <summary>
        /// A Signature which can be compared to other Signatures.
        /// Used to identify corresponding steps across runs.
        /// </summary>
        public IProgramStepSignature Signature;

        /// <summary>
        /// For a non-deterministic boolean choice, the choice taken
        /// </summary>
        public readonly bool? BooleanChoice;

        /// <summary>
        /// For a non-deterministic integer choice, the integer chosen
        /// </summary>
        public readonly int? IntChoice;

        // No need to complicate the edge types
        // Children

        /// <summary>
        /// The next step performed by the handler.
        /// If desired, Successive handlers are also connected by this step.
        /// </summary>
        public ProgramStep NextMachineStep;

        /// <summary>
        /// For Send/Create, The corresponding Receive/Start step.
        /// </summary>
        public ProgramStep CreatedStep;

        /// <summary>
        /// For send steps, the Step which enqueues the message right after the message this step enqueued ( in the same inbox )
        /// For a create step, this is the step which first enqueues into the created machine.
        /// For receive steps, the next Receive operation performed by this machine.
        /// For Start steps, the first Receive operation performed by this machine.
        /// This is needed for Deferred and Ignored events
        /// </summary>
        public ProgramStep NextInboxOrderingStep;

        /// <summary>
        /// For a step which invoked a monitor, the next (in the totally-ordered schedule) step which invoked the same monitor.
        /// </summary>
        public Dictionary<Type, ProgramStep> NextMonitorSteps;

        // Parents

        /// <summary>
        /// The reverse of <see cref="NextMachineStep"/>
        /// </summary>
        public ProgramStep PrevMachineStep;

        /// <summary>
        /// The reverse of <see cref="CreatedStep"/>
        /// </summary>
        public ProgramStep CreatorParent;

        /// <summary>
        /// The reverse of <see cref="NextInboxOrderingStep"/>
        /// </summary>
        public ProgramStep PrevInboxOrderingStep;

        /// <summary>
        /// The reverse of <see cref="NextMonitorSteps"/>
        /// </summary>
        public Dictionary<Type, ProgramStep> PrevMonitorSteps;

        /// <summary>
        /// A hash of the machine state.
        /// </summary>
        public int MachineHash;

        /// <summary>
        /// The step-index in the totally-ordered actual execution of the program.
        /// </summary>
        public int TotalOrderingIndex { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStep"/> class.
        /// </summary>
        /// <param name="opType">Operation type</param>
        /// <param name="srcId">SourceId</param>
        /// <param name="targetId">targetId</param>
        /// <param name="programEventInfo">EventSignature in case this is a send, receive or create, start</param>
        internal ProgramStep(AsyncOperationType opType, ulong srcId, ulong targetId, ProgramStepEventInfo programEventInfo)
        {
            this.ProgramStepType = ProgramStepType.SchedulableStep;
            // this.Operation = op;
            this.BooleanChoice = null;
            this.IntChoice = null;

            this.OpType = opType;
            this.SrcId = srcId;
            this.TargetId = targetId;
            this.EventInfo = programEventInfo; // Just incase
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStep"/> class.
        /// </summary>
        /// <param name="srcId">The Id of the machine taking the choice</param>
        /// <param name="intChoice">The integer chosen non-deterministically</param>
        internal ProgramStep(ulong srcId, int intChoice)
        {
            this.ProgramStepType = ProgramStepType.NonDetIntStep;
            // this.Operation = null;
            this.BooleanChoice = null;
            this.IntChoice = intChoice;

            this.SrcId = srcId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStep"/> class.
        /// </summary>
        /// <param name="srcId">The Id of the machine taking the choice</param>
        /// <param name="boolChoice">The boolean chosen non-deterministically</param>
        internal ProgramStep(ulong srcId, bool boolChoice)
        {
            this.ProgramStepType = ProgramStepType.NonDetBoolStep;
            // this.Operation = null;
            this.BooleanChoice = null;
            this.BooleanChoice = boolChoice;

            this.SrcId = srcId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStep"/> class.
        /// For those special steps
        /// </summary>
        private ProgramStep(ulong srcId, bool? boolChoice, int? intChoice)
        {
            this.ProgramStepType = ProgramStepType.SpecialProgramStepType;
            // this.Operation = null;
            this.SrcId = srcId;
            this.BooleanChoice = boolChoice;
            this.IntChoice = intChoice;
        }

        internal static ProgramStep CreateSpecialProgramStep()
        {
            return new ProgramStep(0, null, null);
        }

        internal static ProgramStep CreateSpecialProgramStep(ulong srcId, bool? boolChoice, int? intChoice )
        {
            ProgramStep step = new ProgramStep(srcId, boolChoice, intChoice);
            return step;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            switch (this.ProgramStepType)
            {
                case ProgramStepType.SchedulableStep:
                    return $"[{this.TotalOrderingIndex}:{this.ProgramStepType}:{this.SrcId}:{this.OpType}:{this.TargetId}]";
                case ProgramStepType.NonDetBoolStep:
                    return $"[{this.TotalOrderingIndex}:{this.ProgramStepType}:{this.SrcId}:{this.BooleanChoice}]";
                case ProgramStepType.NonDetIntStep:
                    return $"[{this.TotalOrderingIndex}:{this.ProgramStepType}:{this.SrcId}:{this.IntChoice}]";
                default:
                    return $"[{this.TotalOrderingIndex}:{this.ProgramStepType}:{base.ToString()}]";
            }
        }

        /// <summary>
        /// Creates a copy of this step, but with no edges set.
        /// </summary>
        /// <returns>A copy of this step</returns>
        public ProgramStep Clone(bool copyTotalOrderingIndex = false)
        {
            ProgramStep newStep = null;
            switch (this.ProgramStepType)
            {
                case ProgramStepType.SchedulableStep:
                    newStep = new ProgramStep(this.OpType, this.SrcId, this.TargetId, this.EventInfo);
                    break;
                case ProgramStepType.NonDetBoolStep:
                    newStep = new ProgramStep(this.SrcId, (bool)this.BooleanChoice);
                    break;
                case ProgramStepType.NonDetIntStep:
                    newStep = new ProgramStep(this.SrcId, (int)this.IntChoice);
                    break;
                case ProgramStepType.SpecialProgramStepType:
                    newStep = CreateSpecialProgramStep(this.SrcId, this.BooleanChoice, this.IntChoice);
                    break;
            }

            // TODO: Do we want to introduce inconsistency by doing
            // newStep.MachineHash = this.MachineHash;
            // Or do we keep it consistent doing this:
            newStep.MachineHash = 0;

            // Fields that may be more complicated
            newStep.Signature = null;

            // HAX: Easy debugging
            newStep.TotalOrderingIndex = copyTotalOrderingIndex ? this.TotalOrderingIndex : 0;

            return newStep;
        }

        /// <summary>
        /// Implements the <see cref="IComparer{T}" /> for ProgramStep.
        /// </summary>
        public class ProgramStepTotalOrderingComparer : IComparer<ProgramStep>
        {
            /// <summary>
            /// Implements the <see cref="IComparer{T}.Compare(T, T)" /> for ProgramStep.
            /// </summary>
            /// <param name="x">The first object</param>
            /// <param name="y">The second object</param>
            /// <returns>-1,0, or 1 depending on whether x is less, equal or greater than y</returns>
            public int Compare(ProgramStep x, ProgramStep y)
            {
                return x.TotalOrderingIndex.CompareTo(y.TotalOrderingIndex);
            }
        }
    }
}
