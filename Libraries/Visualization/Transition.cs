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

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// A P# program transition.
    /// </summary>
    internal struct Transition
    {
        /// <summary>
        /// The origin machine.
        /// </summary>
        public readonly string MachineOrigin;

        /// <summary>
        /// The origin state.
        /// </summary>
        public readonly string StateOrigin;

        /// <summary>
        /// The edge label.
        /// </summary>
        public readonly string EdgeLabel;

        /// <summary>
        /// The target machine.
        /// </summary>
        public readonly string MachineTarget;

        /// <summary>
        /// The target state.
        /// </summary>
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
    }
}