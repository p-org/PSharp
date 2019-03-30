// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring the entry point to a P# program.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EntryPointAttribute : Attribute
    {
    }
}
