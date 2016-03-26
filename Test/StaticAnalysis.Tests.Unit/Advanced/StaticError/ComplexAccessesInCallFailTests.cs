//-----------------------------------------------------------------------
// <copyright file="ComplexAccessesInCallFailTests.cs">
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
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    [TestClass]
    public class ComplexAccessesInCallFailTests : BasePSharpTest
    {
        [TestMethod, Timeout(10000)]
        public void TestComplexAccessesInCallFail()
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
  this.Letter = new Letter("");
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

  this.foo(otherEnvelope);

  this.Send(this.Target, new eUnit(envelope));

  this.bar(otherEnvelope.Letter);
  otherEnvelope.Letter.Text = ""text"";  // ERROR

  envelope = new Envelope();
  this.foobar(envelope, otherEnvelope.Letter);
 }

 void foo(Envelope envelope)
 {
  this.Letter = envelope.Letter;  // ERROR
 }

 void bar(Letter letter)
 {
  letter.Text = ""text2"";  // ERROR
 }

 void foobar(Envelope envelope, Letter letter)
 {
  string str = letter.Text;  // ERROR
  envelope.Id = 5;
 }
}
}";

            var solution = base.GetSolution(test);

            var configuration = Configuration.Create();
            configuration.ProjectName = "Test";
            configuration.Verbose = 2;

            IO.StartWritingToMemory();

            var context = CompilationContext.Create(configuration).LoadSolution(solution);

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... Static analysis detected '4' errors";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            IO.StopWritingToMemory();
        }
    }
}
