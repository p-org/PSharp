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

using System.Runtime.Serialization;

namespace Microsoft.PSharp.TestingServices.Tracing.Error
{
    /// <summary>
    /// Class implementing a P# bug trace traceStep.
    /// </summary>
    [DataContract(IsReference = true)]
    internal sealed class BugTraceStep
    {
        #region fields

        /// <summary>
        /// The unique index of this bug trace traceStep.
        /// </summary>
        internal int Index;

        /// <summary>
        /// The type of this bug trace traceStep.
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
        /// Previous bug trace traceStep.
        /// </summary>
        internal BugTraceStep Previous;

        /// <summary>
        /// Next bug trace traceStep.
        /// </summary>
        internal BugTraceStep Next;

        #endregion

        #region internal methods

        /// <summary>
        /// Creates a bug trace traceStep.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="machine">Machine</param>
        /// <param name="targetMachine">Target machine</param>
        /// <returns>BugTraceStep</returns>
        internal static BugTraceStep GetCreateMachineStep(int index, MachineId machine,
            MachineId targetMachine)
        {
            var traceStep = new BugTraceStep();

            traceStep.Index = index;
            traceStep.Type = BugTraceStepType.CreateMachine;

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
            
            traceStep.TargetMachine = targetMachine.Type;
            traceStep.TargetMachineId = targetMachine.Value;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

        /// <summary>
        /// Creates a bug trace traceStep.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="machine">Machine</param>
        /// <param name="targetMachine">Target machine</param>
        /// <param name="e">Event</param>
        /// <returns>BugTraceStep</returns>
        internal static BugTraceStep GetSendEventStep(int index, MachineId machine,
            MachineId targetMachine, Event e)
        {
            var traceStep = new BugTraceStep();

            traceStep.Index = index;
            traceStep.Type = BugTraceStepType.SendEvent;

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

            traceStep.TargetMachine = targetMachine.Type;
            traceStep.TargetMachineId = targetMachine.Value;

            traceStep.Event = e.GetType().FullName;

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
