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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.Parsing.Tests.Unit
{
    [TestClass]
    public class PSharpDeclarationsParsingFailTests
    {
        [TestMethod]
        public void TestIncorrectUsingDeclaration()
        {
            var test = "" +
                "using System.Text";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \";\".");
        }

        [TestMethod]
        public void TestUsingDeclarationWithoutIdentifier()
        {
            var test = "" +
                "using;";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected identifier.");
        }

        [TestMethod]
        public void TestUnexpectedTokenWithoutNamespace()
        {
            var test = "" +
                "private";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Unexpected token.");
        }

        [TestMethod]
        public void TestNamespaceDeclarationWithoutIdentifier()
        {
            var test = "" +
                "namespace { }";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected namespace identifier.");
        }

        [TestMethod]
        public void TestProtectedEventDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "protected event e;" +
                "}";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Event and machine declarations must be internal or public.");
        }

        [TestMethod]
        public void TestPrivateEventDeclaration()
        {
            var test = "" +
                "namespace Foo {" +
                "private event e;" +
                "}";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Event and machine declarations must be internal or public.");
        }

        [TestMethod]
        public void TestEventDeclarationWithoutNamespace()
        {
            var test = "" +
                "event e;";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Must be declared inside a namespace.");
        }

        [TestMethod]
        public void TestMachineDeclarationWithoutState()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "}" +
                "}";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine must declare at least one state.");
        }

        [TestMethod]
        public void TestMachineDeclarationWithoutIdentifier()
        {
            var test = "" +
                "namespace Foo {" +
                "machine{}" +
                "}";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected machine identifier.");
        }

        [TestMethod]
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

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Unexpected token.");
        }

        [TestMethod]
        public void TestMachineDeclarationWithoutStartState()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "state S { }" +
                "}" +
                "}";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine must declare a start state.");
        }

        [TestMethod]
        public void TestMachineDeclarationWithoutStartState2()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "state S1 { }" +
                "state S2 { }" +
                "}" +
                "}";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine must declare a start state.");
        }

        [TestMethod]
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

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine must declare a start state.");
        }

        [TestMethod]
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

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "A machine can declare only a single start state.");
        }

        [TestMethod]
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

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Duplicate entry declaration.");
        }

        [TestMethod]
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

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Duplicate exit declaration.");
        }

        [TestMethod]
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

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Expected \"{\".");
        }
    }
}
