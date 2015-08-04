//-----------------------------------------------------------------------
// <copyright file="TraceStep.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.PSharp.Exploration
{
    /// <summary>
    /// Class implementing a P# program trace step.
    /// </summary>
    internal sealed class TraceStep
    {
        #region fields

        /// <summary>
        /// The unique index of this trace step.
        /// </summary>
        internal int Index;

        /// <summary>
        /// True if the trace step is a non-deterministic choice.
        /// False if it is a scheduling choice.
        /// </summary>
        internal bool IsChoice { get; private set; }

        /// <summary>
        /// The scheduled machine. Only relevant if this is a scheduling
        /// trace step.
        /// </summary>
        internal Machine ScheduledMachine;

        /// <summary>
        /// The non-deterministic choice id. Only relevant if
        /// this is a choice trace step.
        /// </summary>
        internal string NondetId;

        /// <summary>
        /// The non-deterministic choice value. Only relevant if
        /// this is a choice trace step.
        /// </summary>
        internal bool Choice;

        /// <summary>
        /// Previous trace step.
        /// </summary>
        internal TraceStep Previous;

        /// <summary>
        /// Next trace step.
        /// </summary>
        internal TraceStep Next;

        #endregion

        #region internal API

        /// <summary>
        /// Creates a scheduling choice trace step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="scheduledMachine">Scheduled machine</param>
        /// <returns>TraceStep</returns>
        internal static TraceStep CreateSchedulingChoice(int index, Machine scheduledMachine)
        {
            var traceStep = new TraceStep();

            traceStep.Index = index;
            traceStep.IsChoice = false;
            traceStep.ScheduledMachine = scheduledMachine;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

        /// <summary>
        /// Creates a nondeterministic choice trace step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="uniqueId">Unique id</param>
        /// <param name="choice">Choice</param>
        /// <returns>TraceStep</returns>
        internal static TraceStep CreateNondeterministicChoice(int index, string uniqueId, bool choice)
        {
            var traceStep = new TraceStep();

            traceStep.Index = index;
            traceStep.IsChoice = true;
            traceStep.NondetId = uniqueId;
            traceStep.Choice = choice;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

        #endregion

        #region generic public and override methods

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean value</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            TraceStep mid = obj as TraceStep;
            if (mid == null)
            {
                return false;
            }

            return this.Index == mid.Index;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Index.GetHashCode();
        }

        #endregion
    }
}
