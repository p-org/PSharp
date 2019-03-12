// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class FieldSendingTests : BaseTest
    {
        public FieldSendingTests(ITestOutputHelper output)
            : base(output)
        { }

        [Fact]
        public void TestFieldSending()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public MachineId Id;
 
 public eUnit(MachineId id)
  : base()
 {
  this.Id = id;
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
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(this.Id));
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestBasicFieldSendingViaSend1Fail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public Letter Letter;
 
 public eUnit(Letter letter)
  : base()
 {
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;

 public Letter(string text)
 {
  this.Text = text;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(this.Letter));
 }
}
}";

            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'Letter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestBasicFieldSendingViaSend2Fail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public Letter Letter;
 
 public eUnit(Letter letter)
  : base()
 {
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;

 public Letter(string text)
 {
  this.Text = text;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Letter = new Letter(""test"");
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(this.Letter));
 }
}
}";

            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'Letter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestBasicFieldSendingViaSend3Fail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public MachineId Target;
 public Letter Letter;
 
 public eUnit(MachineId target, Letter letter)
  : base()
 {
  this.Target = target;
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;

 public Letter(string text)
 {
  this.Text = text;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Letter = new Letter(""test"");
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(this.Id, this.Letter));
 }
}
}";

            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'Letter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestBasicFieldSendingViaCreateMachine1Fail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public Letter Letter;
 
 public eUnit(Letter letter)
  : base()
 {
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;

 public Letter(string text)
 {
  this.Text = text;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Letter = new Letter(""test"");
  this.Target = this.CreateMachine(typeof(M), new eUnit(this.Letter));
 }
}
}";

            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'Letter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestBasicFieldSendingViaCreateMachine2Fail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public MachineId Target;
 public Letter Letter;
 
 public eUnit(MachineId target, Letter letter)
  : base()
 {
  this.Target = target;
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;

 public Letter(string text)
 {
  this.Text = text;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Letter = new Letter(""test"");
  this.Target = this.CreateMachine(typeof(M), new eUnit(this.Id, this.Letter));
 }
}
}";

            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'Letter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }
    }
}
