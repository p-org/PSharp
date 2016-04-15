//-----------------------------------------------------------------------
// <copyright file="StateTransition.cs">
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

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A P# state-transition.
    /// </summary>
    internal sealed class StateTransition
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private PSharpAnalysisContext AnalysisContext;

        /// <summary>
        /// The parent state.
        /// </summary>
        private MachineState State;

        /// <summary>
        /// The target state.
        /// </summary>
        internal MachineState TargetState;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="targetState">MachineState</param>
        /// <param name="state">MachineState</param>
        /// <param name="context">AnalysisContext</param>
        internal StateTransition(MachineState targetState, MachineState state,
            PSharpAnalysisContext context)
        {
            this.AnalysisContext = context;
            this.State = state;
            this.TargetState = targetState;
        }

        #endregion
    }
}
