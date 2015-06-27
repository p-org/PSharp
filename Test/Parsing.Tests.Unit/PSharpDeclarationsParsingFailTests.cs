//-----------------------------------------------------------------------
// <copyright file="PSharpDeclarationsParsingFailTests.cs" company="Microsoft">
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
    public class PSharpDeclarationsParsingFailTests
    {
        #region using declarations

        [TestMethod, Timeout(3000)]
        public void TestIncorrectUsingDeclaration()
        {
            var test = "" +
                "using System.Text";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \";\".");
        }

        [TestMethod, Timeout(3000)]
        public void TestUsingDeclarationWithoutIdentifier()
        {
            var test = "" +
                "using;";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected identifier.");
        }

        #endregion

        #region namespace declarations

        [TestMethod, Timeout(3000)]
        public void TestUnexpectedTokenWithoutNamespace()
        {
            var test = "" +
                "private";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Unexpected token.");
        }

        [TestMethod, Timeout(3000)]
        public void TestNamespaceDeclarationWithoutIdentifier()
        {
            var test = "" +
                "namespace { }";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected namespace identifier.");
        }

        #endregion

        #region event declarations

        [TestMethod, Timeout(3000)]
        public void TestProtectedEventDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "protected event e;" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Event and machine declarations must be internal or public.");
        }

        [TestMethod, Timeout(3000)]
        public void TestPrivateEventDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "private event e;" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Event and machine declarations must be internal or public.");
        }

        [TestMethod, Timeout(3000)]
        public void TestEventDeclarationWithoutNamespace()
        {
            var test = "" +
                "event e;";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Must be declared inside a namespace.");
        }

        #endregion

        #region machine declarations

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutState()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine must declare at least one state.");
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutIdentifier()
        {
            var test = "" +
                "namespace Foo {" +
                "machine{}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected machine identifier.");
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithTwoBodies()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S { }" +
                "}" +
                "{" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Unexpected token.");
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutStartState()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "state S { }" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine must declare a start state.");
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutStartState2()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "state S1 { }" +
                "state S2 { }" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine must declare a start state.");
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutStartState3()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "state S1 { }\n" +
                "state S2 { }\n\n" +
                "state S3 { }\n" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine must declare a start state.");
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithMoreThanOneStartState()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1 { }" +
                "start state S2 { }\n\n" +
                "start state S3 { }" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine can declare only a single start state.");
        }

        #endregion

        #region state declarations

        [TestMethod, Timeout(3000)]
        public void TestStateDeclarationWithMoreThanOneEntry()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{\n" +
                "entry {}" +
                "entry{}" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Duplicate entry declaration.");
        }

        [TestMethod, Timeout(3000)]
        public void TestStateDeclarationWithMoreThanOneExit()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{\n" +
                "exit{}" +
                "exit {}" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Duplicate exit declaration.");
        }

        [TestMethod, Timeout(3000)]
        public void TestEntryDeclarationWithUnexpectedIdentifier()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{\n" +
                "entry Bar {}\n" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \"{\".");
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventGotoStateDeclarationWithoutSemicolon()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e goto S2" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \";\".");
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventGotoStateDeclarationWithoutState()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e goto;" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected state identifier.");
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDoActionDeclarationWithoutSemicolon()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e do Bar" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \";\".");
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDoActionDeclarationWithoutAction()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e do;" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected action identifier.");
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDeclarationWithoutHandler()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S1" +
                "{" +
                "on e;" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \"do\", \"goto\" or \"push\".");
        }

        [TestMethod, Timeout(3000)]
        public void TestIgnoreEventDeclarationWithoutComma()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{" +
                "ignore e1 e2;" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \",\".");
        }

        [TestMethod, Timeout(3000)]
        public void TestDeferEventDeclarationWithoutComma()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{" +
                "defer e1 e2;" +
                "}" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \",\".");
        }

        #endregion

        #region field declarations

        [TestMethod, Timeout(3000)]
        public void TestPublicFieldDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "public int k;" +
                "start state S { }" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A field or method cannot be public.");
        }

        [TestMethod, Timeout(3000)]
        public void TestInternalFieldDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "internal int k;" +
                "start state S { }" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A field or method cannot be internal.");
        }

        [TestMethod, Timeout(3000)]
        public void TestIntFieldDeclarationWithoutSemicolon()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "int k" +
                "start state S { }" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \"(\" or \";\".");
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineFieldDeclarationWithoutSemicolon()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "machine N" +
                "start state S { }" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \"(\" or \";\".");
        }

        [TestMethod, Timeout(3000)]
        public void TestPrivateMachineFieldDeclarationWithoutSemicolon()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "private machine N" +
                "start state S { }" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test));

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \"(\" or \";\".");
        }

        #endregion
    }
}
