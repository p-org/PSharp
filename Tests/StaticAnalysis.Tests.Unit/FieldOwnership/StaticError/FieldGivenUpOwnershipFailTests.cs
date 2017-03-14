//-----------------------------------------------------------------------
// <copyright file="FieldGivenUpOwnershipFailTests.cs">
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
    public class FieldGivenUpOwnershipFailTests : BaseTest
    {
        [TestMethod, Timeout(10000)]
        public void TestFieldGivenUpOwnership1Fail()
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
  var letter = new Letter(""test"");
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(letter));
  this.Letter = letter;
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' assigns " +
                "'letter' to field 'Foo.M.Letter' after giving up its ownership.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldGivenUpOwnership2Fail()
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
  this.Letter = new Letter(""London"");
  this.Send(this.Target, new eUnit(this.Letter));
  this.Foo();
 }

 void Foo()
 {
  this.Bar();
 }

 void Bar()
 {
  this.Letter = new Letter(""Bangalore"");
  this.Letter.Text = ""text2"";
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' sends " +
                "'Letter', which contains data from field 'Foo.M.Letter'.";
            base.AssertFailed(configuration, test, 1, error, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldAccessAfterGivenUpOwnership1Fail()
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
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(this.Letter));
  int num = this.Letter.Num;
  this.Letter.Text = ""London"";
 }
}
}";
            
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 3, isPSharpProgram: false);
        }

        [TestMethod, Timeout(10000)]
        public void TestFieldAccessAfterGivenUpOwnership2Fail()
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
  this.Send(this.Target, new eUnit(this.Id, this.Letter));
  int num = this.Letter.Num;
  this.Letter.Text = ""London"";
 }
}
}";
            var configuration = base.GetConfiguration();
            configuration.DoStateTransitionAnalysis = false;
            base.AssertFailed(configuration, test, 3, isPSharpProgram: false);
        }
    }
}
