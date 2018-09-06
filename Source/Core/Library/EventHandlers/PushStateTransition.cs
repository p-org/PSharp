﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.PSharp
{

    /// <summary>
    /// Defines a push state transition.
    /// </summary>
    internal sealed class PushStateTransition 
    {
        /// <summary>
        /// Target state.
        /// </summary>
        public Type TargetState;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PushStateTransition(Type TargetState)
        {
            this.TargetState = TargetState;
        }
    }
}
