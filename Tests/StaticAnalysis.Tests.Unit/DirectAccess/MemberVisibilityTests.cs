// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    public class MemberVisibilityTests : BaseTest
    {
        #region correct tests

        [Fact]
        public void TestMemberVisibility()
        {
            var test = @"
namespace Foo {
class M : Machine
{
 int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Num = 1;
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestPublicFieldVisibility()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class M : Machine
{
 public int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Num = 1;
 }
}
}";
            var error = "Warning: Field 'int Num' of machine 'Foo.M' is declared as " +
                "'public'.   at 'public int Num;' in Program.cs:line 7";
            base.AssertWarning(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestPublicMethodVisibility()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class M : Machine
{
 int Num;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 public void FirstOnEntryAction()
 {
  this.Num = 1;
 }
}
}";
            var error = "Warning: Method 'FirstOnEntryAction' of machine 'Foo.M' is " +
                "declared as 'public'.   at 'FirstOnEntryAction' in Program.cs:line 13";
            base.AssertWarning(test, 1, error, isPSharpProgram: false);
        }

        #endregion
    }
}
