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

        [TestMethod]
        public void TestNamespaceDeclaration()
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

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestNamespaceDeclaration2()
        {
            var test = "";
            test += "namespace Foo { }";
            test += "namespace Bar { }";

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
            var test = "";
            test += "namespace Foo{}";

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
        public void TestEventDeclaration()
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

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestMachineDeclaration()
        {
            var test = "";
            test += "namespace Foo {";
            test += "machine M {";
            test += "start state S { }";
            test += "}";
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
    }
}
