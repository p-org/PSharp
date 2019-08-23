using Microsoft.PSharp.DataFlowAnalysis;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A P# state-transition.
    /// </summary>
    internal sealed class StateTransition
    {
        /// <summary>
        /// The analysis context.
        /// </summary>
        private readonly AnalysisContext AnalysisContext;

        /// <summary>
        /// The parent state.
        /// </summary>
        private readonly MachineState State;

        /// <summary>
        /// The target state.
        /// </summary>
        internal MachineState TargetState;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateTransition"/> class.
        /// </summary>
        internal StateTransition(MachineState targetState, MachineState state, AnalysisContext context)
        {
            this.AnalysisContext = context;
            this.State = state;
            this.TargetState = targetState;
        }
    }
}
