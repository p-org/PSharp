// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class used for representing a
    /// group of related states.
    /// </summary>
    public abstract class StateGroup
    {
        /// <summary>
        /// Returns the qualified (MachineGroup) name of a MachineState
        /// </summary>
        /// <param name="state">State</param>
        /// <returns>Qualified state name</returns>
        internal static string GetQualifiedStateName(Type state)
        {
            var name = state.Name;

            while (state.DeclaringType != null)
            {
                if (!state.DeclaringType.IsSubclassOf(typeof(StateGroup))) break;
                name = string.Format("{0}.{1}", state.DeclaringType.Name, name);
                state = state.DeclaringType;
            }

            return name;
        }
    }
}
