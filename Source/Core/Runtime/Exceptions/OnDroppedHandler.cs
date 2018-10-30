// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Handles the <see cref="PSharpRuntime.OnDropped"/> event.
    /// </summary>
    public delegate void OnDroppedHandler(Event e, MachineId target);
}
