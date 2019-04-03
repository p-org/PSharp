﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for checking liveness properties in monitors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ColdAttribute : Attribute
    {
    }
}
