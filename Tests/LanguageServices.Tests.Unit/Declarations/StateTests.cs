//-----------------------------------------------------------------------
// <copyright file="StateTests.cs">
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

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public class StateTests
    {
        #region correct tests

        [Fact]
        public void TestStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1 { }
state S2 { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        class S1 : MachineState
        {
        }

        class S2 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestEntryDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry{}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S_on_entry_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
exit{}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnExit(nameof(psharp_S_on_exit_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_exit_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestEntryAndExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry {}
exit {}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S_on_entry_action))]
        [OnExit(nameof(psharp_S_on_exit_action))]
        class S : MachineState
        {
        }

        protected void psharp_S_on_entry_action()
        {}

        protected void psharp_S_on_exit_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e), typeof(S2))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnEventGotoStateDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e1 goto S2;
on e2 goto S3;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e1), typeof(S2))]
        [OnEventGotoState(typeof(e2), typeof(S3))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnSimpleGenericEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<int> goto S2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e<int>), typeof(S2))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnComplexGenericEventGotoStateDeclaration()
        {
            var test = @"
using System.Collections.Generic;
namespace Foo {
machine M {
start state S1
{
on e<List<Tuple<bool, object>>, Dictionary<string, float>> goto S2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;
using System.Collections.Generic;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e<List<Tuple<bool,object>>,Dictionary<string,float>>), typeof(S2))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnQualifiedEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on Foo.e goto S2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(Foo.e), typeof(S2))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2 with {}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_S1_e_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnSimpleGenericEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<int> goto S2 with {}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e<int>), typeof(S2), nameof(psharp_S1_e_type_0_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_type_0_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnComplexGenericEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<List<Tuple<bool, object>>, Dictionary<string, float>> goto S2 with {}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e<List<Tuple<bool,object>>,Dictionary<string,float>>), typeof(S2), nameof(psharp_S1_e_type_0_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_type_0_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOMultipleGenericEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2 with {}
on e<int> goto S2 with {}
on e<List<Tuple<bool, object>>, Dictionary<string, float>> goto S2 with {}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_S1_e_action))]
        [OnEventGotoState(typeof(e<int>), typeof(S2), nameof(psharp_S1_e_type_1_action))]
        [OnEventGotoState(typeof(e<List<Tuple<bool,object>>,Dictionary<string,float>>), typeof(S2), nameof(psharp_S1_e_type_2_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_action()
        {}

        protected void psharp_S1_e_type_1_action()
        {}

        protected void psharp_S1_e_type_2_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do Bar;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventDoAction(typeof(e), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnEventDoActionDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e1 do Bar;
on e2 do Baz;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventDoAction(typeof(e1), nameof(Bar))]
        [OnEventDoAction(typeof(e2), nameof(Baz))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnSimpleGenericEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<int> do Bar;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventDoAction(typeof(e<int>), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnComplexGenericEventDoActionDeclaration()
        {
            var test = @"
using System.Collection.Generic;
namespace Foo {
machine M {
start state S1
{
on e<List<Tuple<bool, object>>, Dictionary<string, float>> do Bar;
}
}
}";
            var expected = @"
using Microsoft.PSharp;
using System.Collection.Generic;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventDoAction(typeof(e<List<Tuple<bool,object>>,Dictionary<string,float>>), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnQualifiedEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on Foo.e do Bar;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventDoAction(typeof(Foo.e), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnEventDoActionDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do {}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventDoAction(typeof(e), nameof(psharp_S1_e_action))]
        class S1 : MachineState
        {
        }

        protected void psharp_S1_e_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestOnEventGotoStateAndDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e1 goto S2;
on e2 do Bar;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(e1), typeof(S2))]
        [OnEventDoAction(typeof(e2), nameof(Bar))]
        class S1 : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestIgnoreEventDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [IgnoreEvents(typeof(e))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestIgnoreEventDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e1, e2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [IgnoreEvents(typeof(e1), typeof(e2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestIgnoreEventDeclarationQualifiedComplex()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e1<int>, Foo.e2<Bar.e3>;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [IgnoreEvents(typeof(e1<int>), typeof(Foo.e2<Bar.e3>))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestDeferEventDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [DeferEvents(typeof(e))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestDeferEventDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e1,e2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [DeferEvents(typeof(e1), typeof(e2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestDeferEventDeclarationQualifiedComplex()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e1<int>, halt, default, Foo.e2<Bar.e3>;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [DeferEvents(typeof(e1<int>), typeof(Microsoft.PSharp.Halt), typeof(Microsoft.PSharp.Default), typeof(Foo.e2<Bar.e3>))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestDefaultEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on default goto S2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(Microsoft.PSharp.Default), typeof(S2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestHaltEvent()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on halt goto S2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(Microsoft.PSharp.Halt), typeof(S2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestWildcardEventDefer()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer *;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [DeferEvents(typeof(Microsoft.PSharp.WildCardEvent))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestWildcardEventAction()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
on *,e1.e2 goto S2;
on * push S2;
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEventGotoState(typeof(Microsoft.PSharp.WildCardEvent), typeof(S2))]
        [OnEventGotoState(typeof(e1.e2), typeof(S2))]
        [OnEventPushState(typeof(Microsoft.PSharp.WildCardEvent), typeof(S2))]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion

        #region failure tests

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        #endregion
    }
}
