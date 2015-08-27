//-----------------------------------------------------------------------
// <copyright file="FieldTests.cs" company="Microsoft">
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
    public class FieldTests
    {
        [TestMethod, Timeout(3000)]
        public void TestIntFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
int k;
start state S { }
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
private int k;
[Microsoft.PSharp.Start]
class S : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine N;
start state S { }
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
private MachineId N;
[Microsoft.PSharp.Start]
class S : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestListFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
List<int> k;
start state S { }
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
private List<int> k;
[Microsoft.PSharp.Start]
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
