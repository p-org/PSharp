// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.TestingServices.Tracing.Error
{
    /// <summary>
    /// Class implementing a P# bug trace step.
    /// </summary>
    [DataContract(IsReference = true)]
    public sealed class BugTraceStep
    {
        /// <summary>
        /// The unique index of this bug trace step.
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// The type of this bug trace step.
        /// </summary>
        [DataMember]
        public BugTraceStepType Type { get; private set; }

        /// <summary>
        /// The machine initiating the action.
        /// </summary>
        [DataMember]
        public MachineId Machine { get; internal set; }

        /// <summary>
        /// The machine state.
        /// </summary>
        [DataMember]
        public string MachineState { get; internal set; }

        /// <summary>
        /// Information about the event being sent.
        /// </summary>
        [DataMember]
        internal EventInfo EventInfo;

        /// <summary>
        /// The invoked action.
        /// </summary>
        [DataMember]
        public string InvokedAction { get; internal set; }

        /// <summary>
        /// The target machine.
        /// </summary>
        [DataMember]
        public MachineId TargetMachine { get; internal set; }

        /// <summary>
        /// The taken nondeterministic boolean choice.
        /// </summary>
        [DataMember]
        public bool? RandomBooleanChoice { get; internal set; }

        /// <summary>
        /// The taken nondeterministic integer choice.
        /// </summary>
        [DataMember]
        public int? RandomIntegerChoice { get; internal set; }

        /// <summary>
        /// Extra information that can be used to
        /// enhance the trace reported to the user.
        /// </summary>
        [DataMember]
        public string ExtraInfo { get; internal set; }

        /// <summary>
        /// Previous bug trace step.
        /// </summary>
        public BugTraceStep Previous { get; internal set; }

        /// <summary>
        /// Next bug trace step.
        /// </summary>
        public BugTraceStep Next { get; internal set; }

        /// <summary>
        /// Creates a bug trace step.
        /// </summary>
        internal static BugTraceStep Create(int index, BugTraceStepType type, MachineId machine,
            string machineState, EventInfo eventInfo, MethodInfo action, MachineId targetMachine,
            bool? boolChoice, int? intChoice, string extraInfo)
        {
            var traceStep = new BugTraceStep();

            traceStep.Index = index;
            traceStep.Type = type;

            traceStep.Machine = machine;
            traceStep.MachineState = machineState;

            traceStep.EventInfo = eventInfo;

            if (action != null)
            {
                traceStep.InvokedAction = action.Name;
            }

            traceStep.TargetMachine = targetMachine;
            traceStep.RandomBooleanChoice = boolChoice;
            traceStep.RandomIntegerChoice = intChoice;
            traceStep.ExtraInfo = extraInfo;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is BugTraceStep traceStep)
            {
                return this.Index == traceStep.Index;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => this.Index.GetHashCode();
    }
}
