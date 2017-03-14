//-----------------------------------------------------------------------
// <copyright file="FieldSendingFailTests.cs">
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
    public class FieldSendingFailTests : BaseTest
    {
        [TestMethod, Timeout(10000)]
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

        [TestMethod, Timeout(10000)]
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

        [TestMethod, Timeout(10000)]
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

        [TestMethod, Timeout(10000)]
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

        [TestMethod, Timeout(10000)]
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
