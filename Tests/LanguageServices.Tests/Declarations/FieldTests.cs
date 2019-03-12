// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class FieldTests
    {
        #region correct tests

        [Fact]
        public void TestIntFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
int k;
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        int k;

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestListFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
List<int> k;
start state S { }
}
}";

            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        List<int> k;

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine N;
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        MachineId N;

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineArrayFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine[] MachineArray;
List<machine> MachineList; 
List<machine[]> MachineArrayList; 
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        MachineId[] MachineArray;
        List<MachineId> MachineList;
        List<MachineId[]> MachineArrayList;

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestPublicFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
public int k;
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine field cannot be public.", test);
        }

        [Fact]
        public void TestInternalFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
internal int k;
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine field cannot be internal.", test);
        }

        [Fact]
        public void TestIntFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
int k
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        [Fact]
        public void TestMachineFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
machine N
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        [Fact]
        public void TestPrivateMachineFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
private machine N
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        #endregion
    }
}
