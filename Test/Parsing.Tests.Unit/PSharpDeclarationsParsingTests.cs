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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "using System.Text;\n";

            Assert.AreEqual(expected, output);
        }

        #endregion

        #region namespace declarations

        [TestMethod]
        public void TestNamespaceDeclaration()
        {
            var test = "" +
                "namespace Foo { }";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestNamespaceDeclaration2()
        {
            var test = "" +
                "namespace Foo { }" +
                "namespace Bar { }";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestNamespaceDeclarationCompact()
        {
            var test = "" +
                "namespace Foo{}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class e1 : Event\n" +
                "{\n" +
                " internal e1(params Object[] payload)\n" +
                "  : base(-1, -1, payload)\n" +
                " { }\n" +
                "}\n" +
                "internal class e2 : Event\n" +
                "{\n" +
                " internal e2(params Object[] payload)\n" +
                "  : base(-1, -1, payload)\n" +
                " { }\n" +
                "}\n" +
                "public class e3 : Event\n" +
                "{\n" +
                " internal e3(params Object[] payload)\n" +
                "  : base(-1, -1, payload)\n" +
                " { }\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S1 : MachineState\n" +
                "{\n" +
                "}\n" +
                "class S2 : MachineState\n" +
                "{\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestEntryDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{\n" +
                "entry{}\n" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "protected override void OnEntry()\n" +
                "{\n" +
                "}\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestExitDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{\n" +
                "exit{}\n" +
                "}" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "protected override void OnExit()\n" +
                "{\n" +
                "}\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "protected override void OnEntry()\n" +
                "{\n" +
                "}\n" +
                "protected override void OnExit()\n" +
                "{\n" +
                "}\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S1 : MachineState\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, GotoStateTransitions> DefineGotoStateTransitions()\n" +
                "{\n" +
                " var dict = new System.Collections.Generic.Dictionary<Type, GotoStateTransitions>();\n" +
                "\n" +
                " var s1Dict = new GotoStateTransitions();\n" +
                " s1Dict.Add(typeof(e), typeof(S2));\n" +
                " dict.Add(typeof(S1), s1Dict);\n" +
                "\n" +
                " return dict;\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S1 : MachineState\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, GotoStateTransitions> DefineGotoStateTransitions()\n" +
                "{\n" +
                " var dict = new System.Collections.Generic.Dictionary<Type, GotoStateTransitions>();\n" +
                "\n" +
                " var s1Dict = new GotoStateTransitions();\n" +
                " s1Dict.Add(typeof(e1), typeof(S2));\n" +
                " s1Dict.Add(typeof(e2), typeof(S3));\n" +
                " dict.Add(typeof(S1), s1Dict);\n" +
                "\n" +
                " return dict;\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S1 : MachineState\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, GotoStateTransitions> DefineGotoStateTransitions()\n" +
                "{\n" +
                " var dict = new System.Collections.Generic.Dictionary<Type, GotoStateTransitions>();\n" +
                "\n" +
                " var s1Dict = new GotoStateTransitions();\n" +
                " s1Dict.Add(typeof(e), typeof(S2), () => \n" +
                "{\n" +
                "}\n" +
                ");\n" +
                " dict.Add(typeof(S1), s1Dict);\n" +
                "\n" +
                " return dict;\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S1 : MachineState\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, ActionBindings> DefineActionBindings()\n" +
                "{\n" +
                " var dict = new System.Collections.Generic.Dictionary<Type, ActionBindings>();\n" +
                "\n" +
                " var s1Dict = new ActionBindings();\n" +
                " s1Dict.Add(typeof(e), new Action(Bar));\n" +
                " dict.Add(typeof(S1), s1Dict);\n" +
                "\n" +
                " return dict;\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S1 : MachineState\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, ActionBindings> DefineActionBindings()\n" +
                "{\n" +
                " var dict = new System.Collections.Generic.Dictionary<Type, ActionBindings>();\n" +
                "\n" +
                " var s1Dict = new ActionBindings();\n" +
                " s1Dict.Add(typeof(e1), new Action(Bar));\n" +
                " s1Dict.Add(typeof(e2), new Action(Baz));\n" +
                " dict.Add(typeof(S1), s1Dict);\n" +
                "\n" +
                " return dict;\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S1 : MachineState\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, ActionBindings> DefineActionBindings()\n" +
                "{\n" +
                " var dict = new System.Collections.Generic.Dictionary<Type, ActionBindings>();\n" +
                "\n" +
                " var s1Dict = new ActionBindings();\n" +
                " s1Dict.Add(typeof(e), () => \n" +
                "{\n" +
                "}\n" +
                ");\n" +
                " dict.Add(typeof(S1), s1Dict);\n" +
                "\n" +
                " return dict;\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S1 : MachineState\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, GotoStateTransitions> DefineGotoStateTransitions()\n" +
                "{\n" +
                " var dict = new System.Collections.Generic.Dictionary<Type, GotoStateTransitions>();\n" +
                "\n" +
                " var s1Dict = new GotoStateTransitions();\n" +
                " s1Dict.Add(typeof(e), typeof(S2));\n" +
                " dict.Add(typeof(S1), s1Dict);\n" +
                "\n" +
                " return dict;\n" +
                "}\n" +
                "\n" +
                "protected override System.Collections.Generic.Dictionary<" +
                "Type, ActionBindings> DefineActionBindings()\n" +
                "{\n" +
                " var dict = new System.Collections.Generic.Dictionary<Type, ActionBindings>();\n" +
                "\n" +
                " var s1Dict = new ActionBindings();\n" +
                " s1Dict.Add(typeof(e), new Action(Bar));\n" +
                " dict.Add(typeof(S1), s1Dict);\n" +
                "\n" +
                " return dict;\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "\n protected override System.Collections.Generic.HashSet<Type> DefineIgnoredEvents()\n" +
                " {\n" +
                "  return new System.Collections.Generic.HashSet<Type>\n" +
                "  {\n" +
                "   typeof(e)\n" + 
                "  };\n" +
                " }\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "\n protected override System.Collections.Generic.HashSet<Type> DefineIgnoredEvents()\n" +
                " {\n" +
                "  return new System.Collections.Generic.HashSet<Type>\n" +
                "  {\n" +
                "   typeof(e1),\n" +
                "   typeof(e2)\n" +
                "  };\n" +
                " }\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "\n protected override System.Collections.Generic.HashSet<Type> DefineDeferredEvents()\n" +
                " {\n" +
                "  return new System.Collections.Generic.HashSet<Type>\n" +
                "  {\n" +
                "   typeof(e)\n" +
                "  };\n" +
                " }\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
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
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "\n protected override System.Collections.Generic.HashSet<Type> DefineDeferredEvents()\n" +
                " {\n" +
                "  return new System.Collections.Generic.HashSet<Type>\n" +
                "  {\n" +
                "   typeof(e1),\n" +
                "   typeof(e2)\n" +
                "  };\n" +
                " }\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        #endregion

        #region field declarations

        [TestMethod]
        public void TestFieldDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "int k;" +
                "start state S { }" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "private int k;\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestMachineFieldDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "machine k;" +
                "start state S { }" +
                "}" +
                "}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser().ParseTokens(tokens);

            var output = program.Rewrite();
            var expected = "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Threading.Tasks;\n" +
                "using Microsoft.PSharp;\n" +
                "namespace Foo\n" +
                "{\n" +
                "class M : Machine\n" +
                "{\n" +
                "private MachineId k;\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        #endregion
    }
}
