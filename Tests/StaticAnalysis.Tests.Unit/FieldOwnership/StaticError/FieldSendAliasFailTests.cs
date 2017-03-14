//-----------------------------------------------------------------------
// <copyright file="FieldSendAliasFailTests.cs">
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

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    [TestClass]
    public class FieldSendAliasFailTests : BaseTest
    {
        [TestMethod, Timeout(10000)]
        public void TestFieldSendAlias1Fail()
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
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  this.Foo(letter);
  this.Send(this.Target, new eUnit(letter));
 }

 void Foo(Letter letter)
 {
  this.Letter = letter;
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', which contains data from field 'letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldSendAlias2Fail()
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
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  this.Letter = letter;
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  this.Send(this.Target, new eUnit(letter));
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldSendAlias3Fail()
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
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  letter = this.Letter;
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  this.Send(this.Target, new eUnit(letter));
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldSendAlias4Fail()
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
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  var otherLetter = this.Foo(letter);
  this.Send(this.Target, new eUnit(otherLetter));
 }

 Letter Foo(Letter letter)
 {
   if (letter.Num == 100)
   {
    var otherLetter = new Letter();
    otherLetter = this.Letter; // ERROR
    return otherLetter;
   }
   else
   {
    return this.Letter; // ERROR
   }
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'otherLetter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldSendAlias5Fail()
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
 Letter Letter;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  var letter = new Letter(""test"", 0);
  this.Target = this.CreateMachine(typeof(M));
  var otherLetter = letter;
  letter = this.Foo(letter);
  this.Send(this.Target, new eUnit(letter));
 }

 Letter Foo(Letter letter)
 {
   return this.Bar(letter);
 }

 Letter Bar(Letter letter)
 {
  Letter otherLetter = new Letter(""test"", 0);
  otherLetter = this.Letter; // ERROR
  return otherLetter;
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldSendAlias6Fail()
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
 Object Obj;

 [Start]
 [OnEntry(nameof(FirstOnEntryAction))]
 class First : MachineState { }

 void FirstOnEntryAction()
 {
  this.Target = this.CreateMachine(typeof(M));
  this.Obj = (this.ReceivedEvent as eUnit).Letter;
  this.Send(this.Target, new eUnit((this.ReceivedEvent as eUnit).Letter));
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'ReceivedEvent', which contains data from field 'Foo.M.Obj'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldSendAlias7Fail()
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
  var letter = new Letter(""test"", 0);
  var envelope = new Envelope(new Letter(""test2"", 1));
  this.Target = this.CreateMachine(typeof(M));
  envelope.Letter = this.Foo(this.Letter);
  this.Send(this.Target, new eUnit(envelope));
 }

 Letter Foo(Letter letter)
 {
  return letter;
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'envelope', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }
    }
}
