//-----------------------------------------------------------------------
// <copyright file="StateFailTests.cs">
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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class StateFailTests
    {
        [TestMethod, Timeout(10000)]
        public void TestStateDeclarationWithMoreThanOneEntry()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry {}
entry{}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Duplicate entry declaration.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestStateDeclarationWithMoreThanOneExit()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
exit{}
exit {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Duplicate exit declaration.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestEntryDeclarationWithUnexpectedIdentifier()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry Bar {}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\".", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithoutEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on goto S2
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithCommaError()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e, goto S2
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithoutState()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected state identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithGenericError1()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on <> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithGenericError2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithGenericError3()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<List<int>>> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithoutEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithCommaError()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e, do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithoutAction()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected action identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithIncorrectWildcardUse()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e.* do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithGenericError1()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on <> do Bar;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithGenericError2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e> do Bar;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithGenericError3()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<List<int>>> do Bar;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDeclarationWithoutHandler()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"do\", \"goto\" or \"push\".", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestIgnoreEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e1 e2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \",\".", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestDeferEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e1 e2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \",\".", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestQualifiedHaltEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on Foo.halt goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestQualifiedDefaultEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on Foo.default goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected identifier.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestGenericHaltEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on halt<int> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestGenericDefaultEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on default<int> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Invalid generic expression.", test);
        }

        [TestMethod, Timeout(10000)]
        public void TestIncorrectGenericEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on e<<int> goto S2;
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token inside a generic name.", test);
        }
    }
}
