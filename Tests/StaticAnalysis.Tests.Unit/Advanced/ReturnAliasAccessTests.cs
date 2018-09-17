// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    public class ReturnAliasAccessTests : BaseTest
    {
        #region correct tests

        [Fact]
        public void TestReturnAliasAccess1()
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
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  Letter otherLetter = null;
  this.Send(this.Target, new eUnit(letter));
  otherLetter = this.Foo(letter);
  otherLetter.Num = 1;
 }

 Letter Foo(Letter letter)
 {
  return new Letter(""test"", 0);
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestReturnAliasAccess2()
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
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(letter));
  letter = this.Foo(letter);
  letter.Num = 1;
 }

 Letter Foo(Letter letter)
 {
  return new Letter(""test"", 0);
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestReturnAliasAccess1Fail()
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
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  Letter otherLetter;
  this.Send(this.Target, new eUnit(letter));
  otherLetter = this.Foo(letter);
  otherLetter.Num = 1;
 }

 Letter Foo(Letter letter)
 {
  return letter;
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestReturnAliasAccess2Fail()
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
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(letter));
  letter = this.Foo(letter);
  letter.Num = 1;
 }

 Letter Foo(Letter letter)
 {
  return this.Bar(letter);
 }

 Letter Bar(Letter letter)
 {
  return letter;
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestReturnAliasAccess3Fail()
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
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(letter));
  letter = this.Foo(letter);
  letter.Num = 1;
 }

 Letter Foo(Letter letter)
 {
  var letter2 = letter;
  return letter2;
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestReturnAliasAccess4Fail()
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
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  Letter otherLetter;
  this.Send(this.Target, new eUnit(letter));
  otherLetter = this.Foo(letter);
  otherLetter.Num = 1;
 }

 Letter Foo(Letter letter)
 {
  if (letter.Num == 10)
  {
   return new Letter();
  }
  else
  {
   return letter;
  }
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestReturnAliasAccess5Fail()
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
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  Letter otherLetter;
  this.Send(this.Target, new eUnit(letter));
  otherLetter = this.Bar(letter);
  otherLetter.Num = 1;
 }

 Letter Foo(Letter letter)
 {
  if (letter.Num == 10)
  {
   return new Letter();
  }
  else
  {
   return letter;
  }
 }

 Letter Bar(Letter letter)
 {
  letter = this.Foo(letter);
  return letter;
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestReturnAliasAccess6Fail()
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
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  Letter otherLetter;
  this.Send(this.Target, new eUnit(letter));
  otherLetter = this.Bar(letter);
  otherLetter.Num = 1;
 }

 Letter Foo(Letter letter)
 {
  if (letter.Num == 10)
  {
   return new Letter();
  }
  else
  {
   return letter;
  }
 }

 Letter Bar(Letter letter)
 {
  return this.Foo(letter);
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestReturnAliasAccess7Fail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public Envelope Envelope;
 
 public eUnit(Envelope envelope)
  : base()
 {
  this.Envelope = envelope;
 }
}

struct Envelope
{
 public Letter Letter;

 public Envelope(Letter letter)
 {
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  this.Foo();
 }

 void Foo()
 {
   var letter = new Letter(""test"", 0);
   var envelope = new Envelope(letter);
   envelope.Letter = this.Bar();
   this.Send(this.Target, new eUnit(envelope));
 }

 Letter Bar()
 {
  var letter = this.Letter;
  return letter;
 }
}
}";

            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'Foo' of machine 'Foo.M' sends 'envelope', " +
                "which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestReturnAliasAccess8Fail()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public Envelope Envelope;
 
 public eUnit(Envelope envelope)
  : base()
 {
  this.Envelope = envelope;
 }
}

struct Envelope
{
 public Letter Letter;

 public Envelope(Letter letter)
 {
  this.Letter = letter;
 }
}

struct Letter
{
 public string Text;
 public int Num;

 public Letter(string text, int num)
 {
  this.Text = text;
  this.Num = num;
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
  this.Foo();
 }

 void Foo()
 {
   var letter = new Letter(""test"", 0);
   var envelope = new Envelope(letter);
   envelope.Letter = this.Bar();
   this.Send(this.Target, new eUnit(envelope));
 }

 Letter Bar()
 {
  return this.Letter;
 }
}
}";

            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'Foo' of machine 'Foo.M' sends 'envelope', " +
                "which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        #endregion
    }
}
