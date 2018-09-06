// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;

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
        private AnalysisContext AnalysisContext;

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
            AnalysisContext context)
        {
            this.AnalysisContext = context;
            this.State = state;
            this.TargetState = targetState;
        }

        #endregion
    }
}
