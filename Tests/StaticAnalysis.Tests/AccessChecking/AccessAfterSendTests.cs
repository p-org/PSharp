// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class AccessAfterSendTests : BaseTest
    {
        public AccessAfterSendTests(ITestOutputHelper output)
            : base(output)
        { }

        [Fact]
        public void TestAccessAfterSend()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public int Value;
 
 public eUnit(int value)
  : base()
 {
  this.Value = value;
 }
}

class M : Machine
{
 MachineId Target;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  int value = 0;
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(value));
  value = 1;
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestAccessAfterSendInCallee()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public int Value;
 
 public eUnit(int value)
  : base()
 {
  this.Value = value;
 }
}

class M : Machine
{
 MachineId Target;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  int value = 0;
  this.Target = this.CreateMachine(typeof(M));
  this.Foo(value);
 }

 void Foo(int value)
 {
  this.Send(this.Target, new eUnit(value));
  value = 1;
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }
    }
}
