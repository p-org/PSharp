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
        #region fields

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
        internal string Machine;

        /// <summary>
        /// The id of the machine initiating the action.
        /// </summary>
        [DataMember]
        internal int MachineId;

        /// <summary>
        /// The target machine.
        /// </summary>
        [DataMember]
        internal string TargetMachine;

        /// <summary>
        /// The id of the target machine.
        /// </summary>
        [DataMember]
        internal int TargetMachineId;

        /// <summary>
        /// The event being sent.
        /// </summary>
        [DataMember]
        internal string Event;

        /// <summary>
        /// The invoked action.
        /// </summary>
        [DataMember]
        internal string InvokedAction;

        /// <summary>
        /// The taken nondeterministic choice.
        /// </summary>
        [DataMember]
        internal bool RandomChoice;

        /// <summary>
        /// Previous bug trace step.
        /// </summary>
        internal BugTraceStep Previous;

        /// <summary>
        /// Next bug trace step.
        /// </summary>
        internal BugTraceStep Next;

        #endregion

        #region internal methods

        /// <summary>
        /// Creates a bug trace step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="type">BugTraceStepType</param>
        /// <param name="machine">Machine</param>
        /// <param name="targetMachine">Target machine</param>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="action">MethodInfo</param>
        /// <param name="choice">Choice</param>
        /// <returns>BugTraceStep</returns>
        internal static BugTraceStep Create(int index, BugTraceStepType type, MachineId machine,
            MachineId targetMachine, EventInfo eventInfo, MethodInfo action, bool choice)
        {
            var traceStep = new BugTraceStep();

            traceStep.Index = index;
            traceStep.Type = type;

            if (machine != null)
            {
                traceStep.Machine = machine.Type;
                traceStep.MachineId = machine.Value;
            }
            else
            {
                traceStep.Machine = "unknown";
                traceStep.MachineId = -1;
            }
            
            if (targetMachine != null)
            {
                traceStep.TargetMachine = targetMachine.Type;
                traceStep.TargetMachineId = targetMachine.Value;
            }

            if (eventInfo != null)
            {
                traceStep.Event = eventInfo.EventName;
            }

            if (action != null)
            {
                traceStep.InvokedAction = action.Name;
            }

            traceStep.RandomChoice = choice;

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

        #endregion
    }
}
