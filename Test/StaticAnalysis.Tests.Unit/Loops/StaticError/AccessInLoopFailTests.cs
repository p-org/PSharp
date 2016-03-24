//-----------------------------------------------------------------------
// <copyright file="AccessInLoopFailTests.cs">
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
    public class AccessInLoopFailTests : BasePSharpTest
    {
        [TestMethod, Timeout(3000)]
        public void TestWriteAccessAfterSendInLoopFail()
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
  int k = 10;
  for (int i = 0; i < k; i++)
  {
    k = 2;
    this.Target = this.CreateMachine(typeof(M));
    var letter = new Letter(""test"");
    this.Send(this.Target, new eUnit(letter));
    letter.Text = ""changed"";
        }
  k = 3;
 }
}
}";

            var configuration = Configuration.Create();
            configuration.Verbose = 2;

            IO.StartWritingToMemory();

            var solution = base.GetSolution(test);
            var context = CompilationContext.Create(configuration).LoadSolution(solution);

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();

            var stats = AnalysisErrorReporter.GetStats();
            var expected = "... Static analysis detected '1' error";
            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

            var error = "Error: Method 'FirstOnEntryAction' of machine 'Foo.M' " +
                "accesses 'letter' after giving up its ownership.";
            var actual = IO.GetOutput();

            Assert.AreEqual(error.Replace(Environment.NewLine, string.Empty),
               actual.Substring(0, actual.IndexOf(Environment.NewLine)));

            IO.StopWritingToMemory();
        }
    }
}
