// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Handles the <see cref="PSharpRuntime.OnFailure"/> event.
    /// </summary>
    public delegate void OnFailureHandler(Exception ex);
}
