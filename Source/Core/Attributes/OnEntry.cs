// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring what action to perform
    /// when entering a machine state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OnEntry : Attribute
    {
        /// <summary>
        /// Action name.
        /// </summary>
        internal string Action;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actionName">Action name</param>
        public OnEntry(string actionName)
        {
            this.Action = actionName;
        }
    }
}
