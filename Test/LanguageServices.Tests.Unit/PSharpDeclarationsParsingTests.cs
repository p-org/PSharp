//-----------------------------------------------------------------------
// <copyright file="PSharpDeclarationsParsingTests.cs" company="Microsoft">
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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class PSharpDeclarationsParsingTests
    {
        #region using declarations

        [TestMethod, Timeout(3000)]
        public void TestUsingDeclaration()
        {
            var test = "";
            test += "using System.Text;";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();
            
            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "using System.Text;";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region namespace declarations

        [TestMethod, Timeout(3000)]
        public void TestNamespaceDeclaration()
        {
            var test = "" +
                "namespace Foo { }";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestNamespaceDeclaration2()
        {
            var test = "" +
                "namespace Foo { }" +
                "namespace Bar { }";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestNamespaceDeclarationCompact()
        {
            var test = "" +
                "namespace Foo{}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region event declarations

        [TestMethod, Timeout(3000)]
        public void TestEventDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "event e1;" +
                "internal event e2;" +
                "public event e3;" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class e1 : Event" +
                "{" +
                " internal e1()" +
                "  : base()" +
                " { }" +
                "}" +
                "internal class e2 : Event" +
                "{" +
                " internal e2()" +
                "  : base()" +
                " { }" +
                "}" +
                "public class e3 : Event" +
                "{" +
                " internal e3()" +
                "  : base()" +
                " { }" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region machine declarations

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S { }" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region state declarations

        [TestMethod, Timeout(3000)]
        public void TestStateDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1 { }" +
                "state S2 { }" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "class S2 : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestEntryDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{" +
                "entry{}" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "class S : MachineState" +
                "{" +
                "protected override void OnEntry(){}}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestExitDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{" +
                "exit{}" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "class S : MachineState" +
                "{" +
                "protected override void OnExit(){}}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestEntryAndExitDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{" +
                "entry {}" +
                "exit {}" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "class S : MachineState" +
                "{" +
                "protected override void OnEntry()" +
                "{" +
                "}" +
                "protected override void OnExit()" +
                "{" +
                "}" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventGotoStateDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e goto S2;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[OnEventGotoState(typeof(e), typeof(S2))]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventGotoStateDeclaration2()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e1 goto S2;" +
                "on e2 goto S3;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[OnEventGotoState(typeof(e1), typeof(S2))]" +
                "[OnEventGotoState(typeof(e2), typeof(S3))]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventGotoStateDeclarationWithBody()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e goto S2 with {};" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_S1_e_action))]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "void psharp_S1_e_action()" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDoActionDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e do Bar;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[OnEventDoAction(typeof(e), nameof(Bar))]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDoActionDeclaration2()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e1 do Bar;" +
                "on e2 do Baz;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[OnEventDoAction(typeof(e1), nameof(Bar))]" +
                "[OnEventDoAction(typeof(e2), nameof(Baz))]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDoActionDeclarationWithBody()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e do {};" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[OnEventDoAction(typeof(e), nameof(psharp_S1_e_action))]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "void psharp_S1_e_action()" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventGotoStateAndDoActionDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e1 goto S2;" +
                "on e2 do Bar;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[OnEventGotoState(typeof(e1), typeof(S2))]" +
                "[OnEventDoAction(typeof(e2), nameof(Bar))]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestIgnoreEventDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{" +
                "ignore e;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[IgnoreEvents(typeof(e))]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestIgnoreEventDeclaration2()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{" +
                "ignore e1, e2;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[IgnoreEvents(typeof(e1), typeof(e2))]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestDeferEventDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{" +
                "defer e;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[DeferEvents(typeof(e))]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestDeferEventDeclaration2()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{" +
                "defer e1,e2;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "[DeferEvents(typeof(e1), typeof(e2))]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region field declarations

        [TestMethod, Timeout(3000)]
        public void TestIntFieldDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "int k;" +
                "start state S { }" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "private int k;" +
                "[Microsoft.PSharp.Start]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineFieldDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "machine N;" +
                "start state S { }" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "private MachineId N;" +
                "[Microsoft.PSharp.Start]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestListFieldDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "List<int> k;" +
                "start state S { }" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "private List<int> k;" +
                "[Microsoft.PSharp.Start]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region method declarations

        [TestMethod, Timeout(3000)]
        public void TestVoidMethodDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S { }" +
                "void Bar() { }" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Microsoft.PSharp.Start]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "private void Bar(){ }" +
                "" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion
    }
}
