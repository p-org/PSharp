// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    public class ComplexAccessesInCallTests : BaseTest
    {
        #region failure tests

        [Fact]
        public void TestComplexAccessesInCall1Fail()
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

struct Letter
{
 public string Text;

 public Letter(string text)
 {
  this.Text = text;
 }
}

struct Envelope
{
 public Letter Letter;
 public string Address;
 public int Id;

 public Envelope(string address, int id)
 {
  this.Letter = new Letter("""");
  this.Address = address;
  this.Id = id;
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
  Envelope envelope = new Envelope(""London"", 0);
  Envelope otherEnvelope = envelope;

  this.Foo(otherEnvelope);

  this.Send(this.Target, new eUnit(envelope));

  this.Bar(otherEnvelope.Letter);
  otherEnvelope.Letter.Text = ""text"";  // ERROR

  envelope = new Envelope();
  this.FooBar(envelope, otherEnvelope.Letter);
 }

 void Foo(Envelope envelope)
 {
  this.Letter = envelope.Letter;  // ERROR
 }

 void Bar(Letter letter)
 {
  letter.Text = ""text2"";  // ERROR
 }

 void FooBar(Envelope envelope, Letter letter)
 {
  string str = letter.Text;  // ERROR
  envelope.Id = 5;
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 4, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall2Fail()
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

class OtherClass
{
 AnotherClass AC;

 internal OtherClass(Letter letter)
 {
  this.AC = new AnotherClass(letter);
 }

 internal void Foo()
 {
  AC.Bar();
 }
}

class AnotherClass
{
 Letter Letter;

 internal AnotherClass(Letter letter)
 {
  this.Letter = letter;
 }

 internal void Bar()
 {
  Letter.Text = ""Test""; // ERROR
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
  Letter letter = new Letter(""London"");
  OtherClass oc = new OtherClass(letter);
  this.Send(this.Target, new eUnit(letter));
  oc.Foo();
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 2, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall3Fail()
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

class OtherClass
{
 AnotherClass AC;

 internal void Foo(Letter letter)
 {
  AC = new AnotherClass(letter);
  this.AC.Bar();
 }
}

class AnotherClass
{
 Letter Letter;

 internal AnotherClass(Letter letter)
 {
  this.Letter = letter;
 }

 internal void Bar()
 {
  Letter.Text = ""Test""; // ERROR
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
  Letter letter = new Letter(""London"");
  OtherClass oc = new OtherClass();
  this.Send(this.Target, new eUnit(letter));
  oc.Foo(letter);
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 2, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall4Fail()
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

class OtherClass
{
 AnotherClass AC;

 internal OtherClass(Letter letter)
 {
  AC = new AnotherClass();
  AC.Letter = letter;
 }

 internal void Foo()
 {
  this.AC.Bar();
 }
}

class AnotherClass
{
 internal Letter Letter;

 internal void Bar()
 {
  Letter.Text = ""Test""; // ERROR
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
  Letter letter = new Letter(""London"");
  OtherClass oc = new OtherClass(letter);
  this.Send(this.Target, new eUnit(letter));
  oc.Foo();
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 3, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall5Fail()
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

class OtherClass
{
 AnotherClass AC;

 internal void Foo(Letter letter)
 {
  AC = new AnotherClass();
  AC.Letter = letter;
  this.AC.Bar();
 }
}

class AnotherClass
{
 internal Letter Letter;

 internal void Bar()
 {
  Letter.Text = ""Test""; // ERROR
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
  Letter letter = new Letter(""London"");
  OtherClass oc = new OtherClass();
  this.Send(this.Target, new eUnit(letter));
  oc.Foo(letter);
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 6, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall6Fail()
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

class OtherClass
{
 AnotherClass AC;

 internal void Foo(Letter letter)
 {
  AC = new AnotherClass();
  AC.Letter = letter;
  AC.Letter.Text = ""Test""; // ERROR
 }
}

class AnotherClass
{
 internal Letter Letter;
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
  Letter letter = new Letter(""London"");
  OtherClass oc = new OtherClass();
  this.Send(this.Target, new eUnit(letter));
  oc.Foo(letter);
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 5, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall7Fail()
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

class OtherClass
{
 internal Letter Letter;

 internal void Foo(Letter letter)
 {
  this.Letter = letter; // ERROR
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;
 OtherClass OC;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  this.OC = new OtherClass();
  OC.Foo(letter);
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' assigns 'letter' " +
                "to field 'Foo.OtherClass.Letter' after giving up its ownership.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall8Fail()
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

class OtherClass
{
 internal Letter Letter;

 public OtherClass(Letter letter)
 {
  this.Letter = letter; // ERROR
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;
 OtherClass OC;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  this.OC = new OtherClass(letter);
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' assigns 'letter' " +
                "to field 'Foo.OtherClass.Letter' after giving up its ownership.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall9Fail()
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

class OtherClass
{
 internal Letter Letter;

 public OtherClass(Letter letter)
 {
  this.Letter = letter;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;
 OtherClass OC;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  var oc = new OtherClass(letter);
  this.OC = oc;
  this.Send(this.Target, new eUnit(letter)); // ERROR
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', which contains data from field 'letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall10Fail()
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

class OtherClass
{
 internal Letter Letter;

 public void Foo(Letter letter)
 {
  this.Letter = letter;
 }
}

class M : Machine
{
 MachineId Target;
 Letter Letter;
 OtherClass OC;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  var oc = new OtherClass();
  oc.Foo(letter);
  this.OC = oc;
  this.Send(this.Target, new eUnit(letter)); // ERROR
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', which contains data from field 'letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestComplexAccessesInCall11Fail()
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

class OtherClass
{
 AnotherClass AC;

 public void Foo(Letter letter)
 {
  var ac = new AnotherClass(letter);
  this.AC = ac;
  this.AC.Bar();
 }
}

class AnotherClass
{
 internal Letter Letter;

 public AnotherClass(Letter letter)
 {
  this.Letter = letter;
 }

 public void Bar()
 {
  this.Letter.Text = ""Test""; // ERROR
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
  Letter letter = new Letter(""London"");
  var oc = new OtherClass();
  this.Send(this.Target, new eUnit(letter));
  oc.Foo(letter);
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 2, isPSharpProgram: false);
        }

        #endregion
    }
}
