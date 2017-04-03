//-----------------------------------------------------------------------
// <copyright file="IfStatementTests.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    public class IfStatementTests : BaseTest
    {
        #region correct tests

        [Fact]
        public void TestIfStatement1()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = letter;

  if (num == 0)
  {
   otherLetter = new Letter(""Bangalore"");
  }
  else
  {
   otherLetter = new Letter(""Redmond"");
  }

  this.Send(this.Target, new eUnit(otherLetter));

  letter.Text = ""text"";
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestIfStatement2()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = letter;

  if (num == 0)
  {
   otherLetter = letter;
  }
  else
  {
   otherLetter = new Letter(""Redmond"");
  }

  otherLetter = new Letter(""Bangalore"");

  this.Send(this.Target, new eUnit(otherLetter));

  letter.Text = ""text"";
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestIfStatement1Fail()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = letter;

  if (num == 0)
  {
   otherLetter = new Letter(""Bangalore"");
  }
  else if (num == 1)
  {
   otherLetter = new Letter(""Redmond"");
  }

  this.Send(this.Target, new eUnit(otherLetter));

  letter.Text = ""text"";
 }
}
}";
            base.AssertFailed(test, 1, isPSharpProgram: false);
        }

        [Fact]
        public void TestIfStatement2Fail()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = letter;

  if (num == 0)
   otherLetter = new Letter(""Bangalore"");
  else if (num == 1)
   otherLetter = new Letter(""Redmond"");

  this.Send(this.Target, new eUnit(otherLetter));

  letter.Text = ""text"";
 }
}
}";
            base.AssertFailed(test, 1, isPSharpProgram: false);
        }

        [Fact]
        public void TestIfStatement3Fail()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = new Letter(""Bangalore"");

  if (num == 0)
  {
   otherLetter = letter;
  }
  else
  {
   otherLetter = new Letter(""Redmond"");
  }

  this.Send(this.Target, new eUnit(otherLetter));

  letter.Text = ""text"";
 }
}
}";
            base.AssertFailed(test, 1, isPSharpProgram: false);
        }

        [Fact]
        public void TestIfStatement4Fail()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = new Letter(""Bangalore"");

  if (num == 0)
  {
   otherLetter = new Letter(""Redmond"");
  }
  else
  {
   otherLetter = letter;
  }

  this.Send(this.Target, new eUnit(otherLetter));

  letter.Text = ""text"";
 }
}
}";
            base.AssertFailed(test, 1, isPSharpProgram: false);
        }

        [Fact]
        public void TestIfStatement5Fail()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");

  if (num == 0)
  {
   this.Send(this.Target, new eUnit(letter));
  }
  else
  {
   letter = new Letter(""Redmond"");
  }

  letter.Text = ""text"";
 }
}
}";
            base.AssertFailed(test, 1, isPSharpProgram: false);
        }

        [Fact]
        public void TestIfStatement6Fail()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");

  if (num == 0)
  {
   letter = new Letter(""Redmond"");
  }
  else
  {
   this.Send(this.Target, new eUnit(letter));
  }

  letter.Text = ""text"";
 }
}
}";
            base.AssertFailed(test, 1, isPSharpProgram: false);
        }

        [Fact]
        public void TestIfStatement7Fail()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = new Letter(""Bangalore"");

  if (num == 0)
  {
   otherLetter = new Letter(""Redmond"");
  }
  else
  {
   if (num == 1)
   {
    otherLetter = letter;
   }
   else
   {
    otherLetter = new Letter(""Bangalore"");
   }

   this.Send(this.Target, new eUnit(otherLetter));
  }

  letter.Text = ""text"";
 }
}
}";
            base.AssertFailed(test, 1, isPSharpProgram: false);
        }

        [Fact]
        public void TestIfStatement8Fail()
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

class eReq : Event
{
 public int Value;
 
 public eReq(int value)
  : base()
 {
  this.Value = value;
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
  int num = (this.ReceivedEvent as eReq).Value;

  this.Target = this.CreateMachine(typeof(M));
  Letter letter = new Letter(""London"");
  Letter otherLetter = new Letter(""Bangalore"");

  this.Letter = letter;

  if (num == 0)
  {
   otherLetter = new Letter(""Redmond"");
  }
  else
  {
   otherLetter = letter;
  }

  this.Send(this.Target, new eUnit(otherLetter));

  this.Letter.Text = ""text"";
 }
}
}";
            base.AssertFailed(test, 2, isPSharpProgram: false);
        }

        #endregion
    }
}
