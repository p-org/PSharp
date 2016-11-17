//-----------------------------------------------------------------------
// <copyright file="Transition.cs">
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

namespace Microsoft.PSharp.TestingServices.Coverage
{
    /// <summary>
    /// A P# program transition.
    /// </summary>
    [DataContract]
    public struct Transition
    {
        /// <summary>
        /// The origin machine.
        /// </summary>
        [DataMember]
        public readonly string MachineOrigin;

        /// <summary>
        /// The origin state.
        /// </summary>
        [DataMember]
        public readonly string StateOrigin;

        /// <summary>
        /// The edge label.
        /// </summary>
        [DataMember]
        public readonly string EdgeLabel;

        /// <summary>
        /// The target machine.
        /// </summary>
        [DataMember]
        public readonly string MachineTarget;

        /// <summary>
        /// The target state.
        /// </summary>
        [DataMember]
        public readonly string StateTarget;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machineOrigin">Origin machine</param>
        /// <param name="stateOrigin">Origin state</param>
        /// <param name="edgeLabel">Edge label</param>
        /// <param name="machineTarget">Target machine</param>
        /// <param name="stateTarget">Target state</param>
        public Transition(string machineOrigin, string stateOrigin, string edgeLabel,
            string machineTarget, string stateTarget)
        {
            this.MachineOrigin = machineOrigin;
            this.StateOrigin = stateOrigin;
            this.EdgeLabel = edgeLabel;
            this.MachineTarget = machineTarget;
            this.StateTarget = stateTarget;
        }

        /// <summary>
        /// Pretty print
        /// </summary>
        public override string ToString()
        {
            if(MachineOrigin == MachineTarget)
            {
                return string.Format("{0}: {1} --{2}--> {3}", MachineOrigin, StateOrigin, EdgeLabel, StateTarget);
            }
            return string.Format("({0}, {1}) --{2}--> ({3}, {4})", MachineOrigin, StateOrigin, EdgeLabel, MachineTarget, StateTarget);
        }
    }
}