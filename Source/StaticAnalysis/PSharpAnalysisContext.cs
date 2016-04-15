//-----------------------------------------------------------------------
// <copyright file="PSharpAnalysisContext.cs">
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

using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// The P# static analysis context.
    /// </summary>
    public sealed class PSharpAnalysisContext : AnalysisContext
    {
        #region fields
        
        /// <summary>
        /// Set of state-machines in the project.
        /// </summary>
        internal HashSet<StateMachine> Machines;

        /// <summary>
        /// Dictionary of state transition graphs in the project.
        /// </summary>
        internal Dictionary<StateMachine, StateTransitionGraphNode> StateTransitionGraphs;

        #endregion

        #region public API

        /// <summary>
        /// Create a new state-machine static analysis context.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="project">Project</param>
        /// <returns>StateMachineAnalysisContext</returns>
        public static new PSharpAnalysisContext Create(Project project)
        {
            return new PSharpAnalysisContext(project);
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">Project</param>
        private PSharpAnalysisContext(Project project)
            : base(project)
        {
            this.Machines = new HashSet<StateMachine>();
            this.StateTransitionGraphs = new Dictionary<StateMachine, StateTransitionGraphNode>();
        }

        #endregion
    }
}
