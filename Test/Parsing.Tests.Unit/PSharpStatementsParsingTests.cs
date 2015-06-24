//-----------------------------------------------------------------------
// <copyright file="PSharpStatementsParsingTests.cs" company="Microsoft">
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
    public class PSharpStatementsParsingTests
    {
        #region create statements

        [TestMethod]
        public void TestCreateStatementInState()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "machine N;" +
                "start state S" +
                "{\n" +
                "entry\n" +
                "{" +
                "N = create N ();" +
                "}" +
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
                "private MachineId N;\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "protected override void OnEntry()\n" +
                "{\n" +
                "(this.Machine as M).N = this.CreateMachine(typeof(N));\n" +
                "}\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestCreateStatementInStateUsingThis()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "machine N;" +
                "start state S" +
                "{\n" +
                "entry\n" +
                "{" +
                "this.N = create N ();" +
                "}" +
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
                "private MachineId N;\n" +
                "[Initial]\n" +
                "class S : MachineState\n" +
                "{\n" +
                "protected override void OnEntry()\n" +
                "{\n" +
                "(this.Machine as M).N = this.CreateMachine(typeof(N));\n" +
                "}\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestCreateStatementInStateInLocalScope()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{\n" +
                "entry\n" +
                "{" +
                "machine n = create N ();" +
                "}" +
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
                "MachineId n = this.CreateMachine(typeof(N));\n" +
                "}\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void TestCreateStatementInStateInLocalScope2()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "start state S" +
                "{\n" +
                "entry\n" +
                "{" +
                "machine n = null;" +
                "n = create N ();" +
                "}" +
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
                "MachineId n = null;\n" +
                "n = this.CreateMachine(typeof(N));\n" +
                "}\n" +
                "}\n" +
                "}\n" +
                "}\n";

            Assert.AreEqual(expected, output);
        }

        #endregion
    }
}
