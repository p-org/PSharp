using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class AccessInVirtualMethodTests : BaseTest
    {
        public AccessInVirtualMethodTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = new Envelope();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter);
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod1Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = new SuperEnvelope();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod2Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope;
  if (letter.Text == """")
  {
    envelope = new Envelope();
  }
  else
  {
    envelope = new SuperEnvelope();
  }

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod3Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope;
  if (letter.Text == """")
    envelope = new Envelope();
  else
    envelope = new SuperEnvelope();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod4Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }

 Envelope Foo()
 {
  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return new SuperEnvelope();
  }
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod5Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }

 Envelope Foo()
 {
  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return this.Bar();
  }
 }

 Envelope Bar()
 {
  return new SuperEnvelope();
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod6Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }

 Envelope Foo()
 {
  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return this.Bar();
  }
 }

 Envelope Bar()
 {
  Envelope envelope = new SuperEnvelope();
  return envelope;
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod7Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }

 internal virtual void Bar(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore"";
 }

 internal override void Bar(Letter letter)
 {
  base.Letter.Text = ""Bangalore"";
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  envelope.Foo(letter); // ERROR
 }

 Envelope Foo()
 {
  Envelope envelope = new SuperEnvelope();
  Envelope anotherEnvelope = new Envelope();
  anotherEnvelope = envelope;

  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return anotherEnvelope;
  }
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod8Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  letter.Text = ""Bangalore""; // ERROR
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  var letter = new Letter(""London"");
  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  this.Bar(envelope, letter);
 }

 Envelope Foo()
 {
  Envelope someEnvelope = new SuperEnvelope();
  Envelope anotherEnvelope;
  anotherEnvelope = new Envelope();
  anotherEnvelope = someEnvelope;

  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return anotherEnvelope;
  }
 }

 void Bar(Envelope envelope, Letter letter)
 {
  this.FooBar(envelope, letter);
 }

 void FooBar(Envelope envelope, Letter letter)
 {
  envelope.Foo(letter);
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' accesses " +
                "'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestAccessInVirtualMethod9Fail()
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

internal class Envelope
{
 internal Letter Letter;

 internal virtual void Foo(Letter letter) { }
}

internal class SuperEnvelope : Envelope
{
 internal override void Foo(Letter letter)
 {
  Letter = letter; // ERROR
  base.Letter.Text = ""Bangalore""; // ERROR
 }
}

class M : Machine
{
 MachineId Target;
 bool Check;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  var letter = new Letter(""London"");
  Envelope envelope = this.Foo();

  this.Send(this.Target, new eUnit(letter));

  this.Bar(envelope, letter);
 }

 Envelope Foo()
 {
  Envelope someEnvelope = new SuperEnvelope();
  Envelope anotherEnvelope;
  anotherEnvelope = new Envelope();
  anotherEnvelope = someEnvelope;

  if (this.Check)
  {
   return new Envelope();
  }
  else
  {
   return anotherEnvelope;
  }
 }

 void Bar(Envelope envelope, Letter letter)
 {
  this.FooBar(envelope, letter);
 }

 void FooBar(Envelope envelope, Letter letter)
 {
  envelope.Foo(letter);
 }
}
}";

            var configuration = Setup.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            Assert.Failed(configuration, test, 3, isPSharpProgram: false);
        }
    }
}
