//-----------------------------------------------------------------------
// <copyright file="AccessAfterSendTests.cs">
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
    public class AccessAfterSendTests : BaseTest
    {
        #region correct tests

        [Fact]
        public void TestAccessAfterSend()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public int Value;
 
 public eUnit(int value)
  : base()
 {
  this.Value = value;
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
  int value = 0;
  this.Target = this.CreateMachine(typeof(M));
  this.Send(this.Target, new eUnit(value));
  value = 1;
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }

        [Fact]
        public void TestAccessAfterSendInCallee()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo {
class eUnit : Event
{
 public int Value;
 
 public eUnit(int value)
  : base()
 {
  this.Value = value;
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
  int value = 0;
  this.Target = this.CreateMachine(typeof(M));
  this.Foo(value);
 }

 void Foo(int value)
 {
  this.Send(this.Target, new eUnit(value));
  value = 1;
 }
}
}";
            base.AssertSucceeded(test, isPSharpProgram: false);
        }

        #endregion
    }
}
