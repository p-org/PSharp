﻿using System;

namespace Microsoft.PSharp
{
#pragma warning disable SA1402 // FileMayOnlyContainASingleType
    /// <summary>
    /// Attribute for declaring the entry point to
    /// a P# program test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring the initialization
    /// method to be called before testing starts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestInitAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring a cleanup method to be
    /// called when all test iterations terminate.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestDisposeAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring a cleanup method to be
    /// called when each test iteration terminates.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestIterationDisposeAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring the factory method that creates
    /// the P# testing runtime. This is an advanced feature,
    /// only to be used for replacing the original P# testing
    /// runtime with an alternative implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class TestRuntimeCreateAttribute : Attribute
    {
    }
#pragma warning restore SA1402 // FileMayOnlyContainASingleType
}
