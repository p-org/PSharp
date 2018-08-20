//-----------------------------------------------------------------------
// <copyright file="TestAttributes.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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

    /// <summary>
    /// Attribute for declaring the type of the P# testing runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class TestRuntimeGetType : Attribute { }

    /// <summary>
    /// Attribute for declaring the default in-memory logger of the P# testing runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class TestRuntimeGetInMemoryLogger : Attribute { }

    /// <summary>
    /// Attribute for declaring the default disposing logger of the P# testing runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class TestRuntimeGetDisposingLogger : Attribute { }
}
