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

namespace Microsoft.PSharp.Parsing.Tests.Unit
{
    [TestClass]
    public class PSharpDeclarationsParsingTests
    {
        #region using declarations

        [TestMethod]
        public void TestUsingDeclaration()
        {
            var test = "";
            test += "using System.Text;";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();
            
            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "using System.Text;";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region namespace declarations

        [TestMethod]
        public void TestNamespaceDeclaration()
        {
            var test = "" +
                "namespace Foo { }";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
        public void TestNamespaceDeclaration2()
        {
            var test = "" +
                "namespace Foo { }" +
                "namespace Bar { }";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
        public void TestNamespaceDeclarationCompact()
        {
            var test = "" +
                "namespace Foo{}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
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

        [TestMethod]
        public void TestEventDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "event e1;" +
                "internal event e2;" +
                "public event e3;" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class e1 : Event" +
                "{" +
                " internal e1()" +
                "  : base(-1, -1)" +
                " { }" +
                "}" +
                "internal class e2 : Event" +
                "{" +
                " internal e2()" +
                "  : base(-1, -1)" +
                " { }" +
                "}" +
                "public class e3 : Event" +
                "{" +
                " internal e3()" +
                "  : base(-1, -1)" +
                " { }" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region machine declarations

        [TestMethod]
        public void TestMachineDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S { }" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region state declarations

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
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

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                "protected override void OnEntry(){}}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                "protected override void OnExit(){}}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
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

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, GotoStateTransitions> DefineGotoStateTransitions()" +
                "{" +
                " var dict = new System.Collections.Generic.Dictionary<Type, GotoStateTransitions>();" +
                "" +
                " var s1Dict = new GotoStateTransitions();" +
                " s1Dict.Add(typeof(e), typeof(S2));" +
                " dict.Add(typeof(S1), s1Dict);" +
                "" +
                " return dict;" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, GotoStateTransitions> DefineGotoStateTransitions()" +
                "{" +
                " var dict = new System.Collections.Generic.Dictionary<Type, GotoStateTransitions>();" +
                "" +
                " var s1Dict = new GotoStateTransitions();" +
                " s1Dict.Add(typeof(e1), typeof(S2));" +
                " s1Dict.Add(typeof(e2), typeof(S3));" +
                " dict.Add(typeof(S1), s1Dict);" +
                "" +
                " return dict;" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, GotoStateTransitions> DefineGotoStateTransitions()" +
                "{" +
                " var dict = new System.Collections.Generic.Dictionary<Type, GotoStateTransitions>();" +
                "" +
                " var s1Dict = new GotoStateTransitions();" +
                " s1Dict.Add(typeof(e), typeof(S2), () => " +
                "{" +
                "}" +
                ");" +
                " dict.Add(typeof(S1), s1Dict);" +
                "" +
                " return dict;" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, ActionBindings> DefineActionBindings()" +
                "{" +
                " var dict = new System.Collections.Generic.Dictionary<Type, ActionBindings>();" +
                "" +
                " var s1Dict = new ActionBindings();" +
                " s1Dict.Add(typeof(e), new Action(Bar));" +
                " dict.Add(typeof(S1), s1Dict);" +
                "" +
                " return dict;" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, ActionBindings> DefineActionBindings()" +
                "{" +
                " var dict = new System.Collections.Generic.Dictionary<Type, ActionBindings>();" +
                "" +
                " var s1Dict = new ActionBindings();" +
                " s1Dict.Add(typeof(e1), new Action(Bar));" +
                " s1Dict.Add(typeof(e2), new Action(Baz));" +
                " dict.Add(typeof(S1), s1Dict);" +
                "" +
                " return dict;" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, ActionBindings> DefineActionBindings()" +
                "{" +
                " var dict = new System.Collections.Generic.Dictionary<Type, ActionBindings>();" +
                "" +
                " var s1Dict = new ActionBindings();" +
                " s1Dict.Add(typeof(e), () => " +
                "{" +
                "}" +
                ");" +
                " dict.Add(typeof(S1), s1Dict);" +
                "" +
                " return dict;" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
        public void TestOnEventGotoStateAndDoActionDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e goto S2;" +
                "on e do Bar;" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S1 : MachineState" +
                "{" +
                "}" +
                "" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, GotoStateTransitions> DefineGotoStateTransitions()" +
                "{" +
                " var dict = new System.Collections.Generic.Dictionary<Type, GotoStateTransitions>();" +
                "" +
                " var s1Dict = new GotoStateTransitions();" +
                " s1Dict.Add(typeof(e), typeof(S2));" +
                " dict.Add(typeof(S1), s1Dict);" +
                "" +
                " return dict;" +
                "}" +
                "" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, ActionBindings> DefineActionBindings()" +
                "{" +
                " var dict = new System.Collections.Generic.Dictionary<Type, ActionBindings>();" +
                "" +
                " var s1Dict = new ActionBindings();" +
                " s1Dict.Add(typeof(e), new Action(Bar));" +
                " dict.Add(typeof(S1), s1Dict);" +
                "" +
                " return dict;" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                " protected override System.Collections.Generic.HashSet<Type> DefineIgnoredEvents()" +
                " {" +
                "  return new System.Collections.Generic.HashSet<Type>" +
                "  {" +
                "   typeof(e)" + 
                "  };" +
                " }" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                " protected override System.Collections.Generic.HashSet<Type> DefineIgnoredEvents()" +
                " {" +
                "  return new System.Collections.Generic.HashSet<Type>" +
                "  {" +
                "   typeof(e1)," +
                "   typeof(e2)" +
                "  };" +
                " }" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                " protected override System.Collections.Generic.HashSet<Type> DefineDeferredEvents()" +
                " {" +
                "  return new System.Collections.Generic.HashSet<Type>" +
                "  {" +
                "   typeof(e)" +
                "  };" +
                " }" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                " protected override System.Collections.Generic.HashSet<Type> DefineDeferredEvents()" +
                " {" +
                "  return new System.Collections.Generic.HashSet<Type>" +
                "  {" +
                "   typeof(e1)," +
                "   typeof(e2)" +
                "  };" +
                " }" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region field declarations

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "private int k;" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "private MachineId N;" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "private List<int> k;" +
                "[Initial]" +
                "class S : MachineState" +
                "{" +
                "}" +
                "}" +
                "}";

            Assert.AreEqual(expected, program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region method declarations

        [TestMethod]
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
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test)).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = "using System;" +
                "using Microsoft.PSharp;" +
                "namespace Foo" +
                "{" +
                "class M : Machine" +
                "{" +
                "[Initial]" +
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
