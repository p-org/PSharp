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

using System;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class PSharpStatementsParsingTests
    {
        #region create statements

        [TestMethod, Timeout(3000)]
        public void TestCreateStatementInState()
        {
            var test = @"
namespace Foo {
machine N {
start state S
{
}
}
machine M {
machine N;
start state S
{
entry
{
N = create(N);
}
}
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
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
protected override void OnEntry()
{
(this.Machine as M).N = this.CreateMachine(typeof(N));
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestCreateStatementInStateUsingThis()
        {
            var test = @"
namespace Foo {
machine M {
machine N;
start state S
{
entry
{
this.N = create N ();
}
}
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
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
protected override void OnEntry()
{
(this.Machine as M).N = this.CreateMachine(typeof(N));
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestCreateStatementInStateInLocalScope()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry
{
machine n = create N ();
}
}
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
class S : MachineState
{
protected override void OnEntry()
{
MachineId n = this.CreateMachine(typeof(N));
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestCreateStatementInStateInLocalScope2()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry
{
machine n = null;
n = create N ();
}
}
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
class S : MachineState
{
protected override void OnEntry()
{
MachineId n = null;
n = this.CreateMachine(typeof(N));
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region while statements

        [TestMethod, Timeout(3000)]
        public void TestWhileStatement()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Bar()
{
while (true)
{
}
}
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
class S : MachineState
{
}
private void Bar()
{
while (true)
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region break statements

        [TestMethod, Timeout(3000)]
        public void TestBreakStatement()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Bar()
{
while (true)
{break;}
}
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = @"using System;
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
class S : MachineState
{
}
private void Bar()
{
while (true)
{
break;
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion

        #region continue statements

        [TestMethod, Timeout(3000)]
        public void TestContinueStatement()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Bar()
{
while (true)
{continue;}
}
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false).
                ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
class S : MachineState
{
}
private void Bar()
{
while (true)
{
continue;
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        #endregion
    }
}
