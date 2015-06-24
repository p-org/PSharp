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
        [TestMethod]
        public void TestNamespaceDeclarationSyntaxParsing()
        {
            var test = "";
            test += "namespace Foo { }";

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

            Assert.AreEqual(output, expected);
        }

        [TestMethod]
        public void TestUnexpectedTokenWithoutNamespaceParsingFail()
        {
            var test = "";
            test += "private";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Unexpected token.",
                parser.GetParsingErrorLog());
        }

        [TestMethod]
        public void TestEventDeclarationSyntaxParsing()
        {
            var test = "";
            test += "namespace Foo {";
            test += "event e1;";
            test += "internal event e2;";
            test += "public event e3;";
            test += "}";

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

            Assert.AreEqual(output, expected);
        }

        [TestMethod]
        public void TestProtectedEventDeclarationSyntaxParsingFail()
        {
            var test = "";
            test += "namespace Foo {";
            test += "protected event e;";
            test += "}";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Event and machine declarations must be internal or public.",
                parser.GetParsingErrorLog());
        }

        [TestMethod]
        public void TestPrivateEventDeclarationSyntaxParsingFail()
        {
            var test = "";
            test += "namespace Foo {";
            test += "private event e;";
            test += "}";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Event and machine declarations must be internal or public.",
                parser.GetParsingErrorLog());
        }

        [TestMethod]
        public void TestEventWithoutNamespaceParsingFail()
        {
            var test = "";
            test += "event e;";

            var parser = new PSharpParser();

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Must be declared inside a namespace.",
                parser.GetParsingErrorLog());
        }
    }
}
