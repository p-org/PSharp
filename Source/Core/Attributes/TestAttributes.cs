// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring the entry point to
    /// a P# program test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class Test : Attribute { }

    /// <summary>
    /// Attribute for declaring the initialization
    /// method to be called before testing starts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestInit : Attribute { }

    /// <summary>
    /// Attribute for declaring a cleanup method to be
    /// called when all test iterations terminate.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestDispose : Attribute { }

    /// <summary>
    /// Attribute for declaring a cleanup method to be
    /// called when each test iteration terminates.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestIterationDispose : Attribute { }

    /// <summary>
    /// Attribute for declaring the factory method that creates
    /// the P# testing runtime. This is an advanced feature,
    /// only to be used for replacing the original P# testing
    /// runtime with an alternative implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class TestRuntimeCreate : Attribute { }
}
