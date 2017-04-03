//-----------------------------------------------------------------------
// <copyright file="DuplicateSendsTests.cs">
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
    public class DuplicateSendsTests : BaseTest
    {
        #region failure tests

        [Fact]
        public void TestDuplicateSends1Fail()
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
  Letter letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  this.Send(this.Target, new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestDuplicateSends2Fail()
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
  Letter letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  this.Send(this.Target, new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestDuplicateSends3Fail()
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
  Letter letter = new Letter(""London"");
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
  this.Send(this.Target, new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestDuplicateSends4Fail()
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
  Letter letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        [Fact]
        public void TestDuplicateSends5Fail()
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
  Letter letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(letter));
  this.Foo(letter);
 }

 void Foo(Letter letter)
 {
  this.Target = this.CreateMachine(typeof(M), new eUnit(letter));
 }
}
}";
            
            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'letter', the ownership of which has already been given up.";
            base.AssertFailed(test, 1, error, isPSharpProgram: false);
        }

        #endregion
    }
}
