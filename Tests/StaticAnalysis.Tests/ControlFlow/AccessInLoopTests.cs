using Microsoft.PSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    public class AccessInLoopTests : BaseTest
    {
        public AccessInLoopTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop1()
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  int k = 10;
  for (int i = 0; i < k; i++)
  {
    letter.Text = ""Bangalore"";
    this.Send(this.Target, new eUnit(letter));
    break;
  }
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop2()
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  int k = 10;
  for (int i = 0; i < k; i++)
  {
    continue;
    letter.Text = ""Bangalore"";
    this.Send(this.Target, new eUnit(letter));
  }
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop3()
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

  int k = 10;
  for (int i = 0; i < k; i++)
  {
    var letter = new Letter(""London"");
    letter.Text = ""Bangalore"";
    this.Send(this.Target, new eUnit(letter));
  }
 }
}
}";
            Assert.Succeeded(test, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop1Fail()
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  int k = 10;
  for (int i = 0; i < k; i++)
  {
    this.Send(this.Target, new eUnit(letter)); // ERROR
  }
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop2Fail()
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  int k = 10;
  for (int i = 0; i < k; i++)
  {
    this.Send(this.Target, new eUnit(letter)); // ERROR
  }

  letter.Text = ""Bangalore""; // ERROR
 }
}
}";
            Assert.Failed(test, 2, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop3Fail()
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  int k = 10;
  for (int i = 0; i < k; i++)
  {
    letter.Text = ""Bangalore""; // ERROR
    this.Send(this.Target, new eUnit(letter)); // ERROR
  }
 }
}
}";
            Assert.Failed(test, 2, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop4Fail()
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  int k = 0;
  while (k < 10)
  {
    this.Send(this.Target, new eUnit(letter)); // ERROR
    k++;
  }
  
  letter.Text = ""Bangalore""; // ERROR
 }
}
}";
            Assert.Failed(test, 2, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop5Fail()
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  int k = 0;
  while (k < 10)
  {
    letter.Text = ""Bangalore""; // ERROR
    this.Send(this.Target, new eUnit(letter)); // ERROR
    k++;
  }
 }
}
}";
            Assert.Failed(test, 2, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop6Fail()
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  int k = 0;
  do
  {
    this.Send(this.Target, new eUnit(letter)); // ERROR
    k++;
  }
  while (k < 10);
  
  letter.Text = ""Bangalore""; // ERROR
 }
}
}";
            Assert.Failed(test, 2, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop7Fail()
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  int k = 0;
  do
  {
    letter.Text = ""Bangalore""; // ERROR
    this.Send(this.Target, new eUnit(letter)); // ERROR
    k++;
  }
  while (k < 10);
 }
}
}";
            Assert.Failed(test, 2, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop8Fail()
        {
            var test = @"
using System.Collection.Generic;
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  List<int> dummies = new List<int>();
  foreach (var dummy in dummies)
  {
    this.Send(this.Target, new eUnit(letter)); // ERROR
  }
  
  letter.Text = ""Bangalore""; // ERROR
 }
}
}";
            Assert.FailedAndWarning(test, 2, 1, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop9Fail()
        {
            var test = @"
using System.Collection.Generic;
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
  var letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M));

  List<int> dummies = new List<int>();
  foreach (var dummy in dummies)
  {
    letter.Text = ""Bangalore""; // ERROR
    this.Send(this.Target, new eUnit(letter)); // ERROR
  }
 }
}
}";
            Assert.FailedAndWarning(test, 2, 1, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop10Fail()
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
  
  int k = 10;
  for (int i = 0; i < k; i++)
  {
    var letter = new Letter(""London"");
    this.Send(this.Target, new eUnit(letter));
    letter.Text = ""Bangalore"";
  }
 }
}
}";
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            Assert.Failed(test, 1, error, isPSharpProgram: false);
        }

        [Fact(Timeout=5000)]
        public void TestWriteAccessAfterSendInLoop11Fail()
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

  int k = 10;
  for (int i = 0; i < k; i++)
  {
    var letter2 = letter;
    letter.Text = ""Bangalore"";
    this.Send(this.Target, new eUnit(letter2));
  }
 }
}
}";
            Assert.Failed(test, 2, isPSharpProgram: false);
        }
    }
}
