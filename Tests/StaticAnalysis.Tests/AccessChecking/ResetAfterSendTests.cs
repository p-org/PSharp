// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class ResetAfterSendTests : BaseTest
    {
        public ResetAfterSendTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void TestResetGivenUpReferenceAfterSend1()
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

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = new Letter(""Bangalore"");
  var text = letter.Text;
 }
}
}";

            var configuration = GetConfiguration();
            configuration.ProjectName = "Test";
            configuration.ThrowInternalExceptions = true;
            configuration.Verbose = 2;
            configuration.AnalyzeDataRaces = true;
            AssertSucceeded(configuration, test, isPSharpProgram: false);
        }

        [Fact]
        public void TestResetGivenUpReferenceAfterSend2()
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

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""test"");
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(letter));
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  letter = new Letter(""test2"");
  letter.Text = ""changed"";
 }
}
}";
            AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestResetViaFieldAfterSend1()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  this.Letter = letter;
 }
}
}";
            AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestResetViaFieldAfterSend2()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  this.Letter.Text = ""Bangalore"";
 }
}
}";
            AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestResetViaFieldAfterSend3()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  letter.Text = ""Bangalore"";
 }
}
}";
            AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestResetViaFieldAfterSend4()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  var text = this.Letter.Text;
 }
}
}";
            AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestResetViaFieldAfterSend5()
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
  var letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  letter = this.Letter;
  var text = letter.Text;
 }
}
}";
            AssertSucceeded(test, isPSharpProgram: false);
        }
    }
}
