//-----------------------------------------------------------------------
// <copyright file="BugTraceStep.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System.Reflection;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.TestingServices.Tracing.Error
{
    /// <summary>
    /// Class implementing a P# bug trace step.
    /// </summary>
    [DataContract(IsReference = true)]
    internal sealed class BugTraceStep
    {
        /// <summary>
        /// The unique index of this bug trace step.
        /// </summary>
        internal int Index;

        /// <summary>
        /// The type of this bug trace step.
        /// </summary>
        [DataMember]
        internal BugTraceStepType Type { get; private set; }

        /// <summary>
        /// The machine initiating the action.
        /// </summary>
        [DataMember]
        internal MachineId Machine;

        /// <summary>
        /// The machine state.
        /// </summary>
        [DataMember]
        internal string MachineState;

        /// <summary>
        /// Information about the event being sent.
        /// </summary>
        [DataMember]
        internal EventInfo EventInfo;

        /// <summary>
        /// The invoked action.
        /// </summary>
        [DataMember]
        internal string InvokedAction;

        /// <summary>
        /// The target machine.
        /// </summary>
        [DataMember]
        internal MachineId TargetMachine;

        /// <summary>
        /// The taken nondeterministic boolean choice.
        /// </summary>
        [DataMember]
        internal bool? RandomBooleanChoice;

        /// <summary>
        /// The taken nondeterministic integer choice.
        /// </summary>
        [DataMember]
        internal int? RandomIntegerChoice;

        /// <summary>
        /// Extra information that can be used to
        /// enhance the trace reported to the user.
        /// </summary>
        [DataMember]
        internal string ExtraInfo;

        /// <summary>
        /// Previous bug trace step.
        /// </summary>
        internal BugTraceStep Previous;

        /// <summary>
        /// Next bug trace step.
        /// </summary>
        internal BugTraceStep Next;

        /// <summary>
        /// Creates a bug trace step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="type">BugTraceStepType</param>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="action">MethodInfo</param>
        /// <param name="targetMachine">Target machine</param>
        /// <param name="boolChoice">Boolean choice</param>
        /// <param name="intChoice">Integer choice</param>
        /// <param name="extraInfo">Extra info</param>
        /// <returns>BugTraceStep</returns>
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
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            BugTraceStep traceStep = obj as BugTraceStep;
            if (traceStep == null)
            {
                return false;
            }

            return this.Index == traceStep.Index;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Index.GetHashCode();
        }
    }
}
