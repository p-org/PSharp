//-----------------------------------------------------------------------
// <copyright file="StateTests.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class StateTests
    {
        [TestMethod, Timeout(3000)]
        public void TestStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1 { }
state S2 { }
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
class S : MachineState
{
protected override void OnEntry(){}}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
class S : MachineState
{
protected override void OnExit(){}}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
class S : MachineState
{
protected override void OnEntry()
{
}
protected override void OnExit()
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2 with {};
}
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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
void psharp_S1_e_action()
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDoActionDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do {};
}
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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
void psharp_S1_e_action()
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
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

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
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

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }
    }
}
