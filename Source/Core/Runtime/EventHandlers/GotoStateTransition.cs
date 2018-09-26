// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Defines a goto state transition.
    /// </summary>
    internal sealed class GotoStateTransition 
    {
        /// <summary>
        /// Target state.
        /// </summary>
        public Type TargetState;

        /// <summary>
        /// An optional lambda function, which can execute after
        /// the default OnExit function of the exiting state.
        /// </summary>
        public string Lambda;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GotoStateTransition(Type TargetState, string Lambda)
        {
            this.TargetState = TargetState;
            this.Lambda = Lambda;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GotoStateTransition(Type TargetState)
        {
            this.TargetState = TargetState;
            this.Lambda = null;
        }
    }
}
