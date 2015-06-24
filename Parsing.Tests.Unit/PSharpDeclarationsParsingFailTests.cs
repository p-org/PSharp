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
