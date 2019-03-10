// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class MethodTests
    {
        #region correct tests

        [Fact]
        public void TestVoidMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Bar() { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }

        void Bar()
        { }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestVoidMethodDeclaration2()
        {
            var test = @"
namespace Foo {
machine M { start state S { } void Bar() { } }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }

        void Bar()
        { }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestPublicMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
public void Foo() { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine method cannot be public.", test);
        }

        [Fact]
        public void TestInternalMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
internal void Foo() { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine method cannot be internal.", test);
        }

        [Fact]
        public void TestMethodDeclarationWithoutBrackets()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Foo()
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\" or \";\".", test);
        }

        #endregion
    }
}
