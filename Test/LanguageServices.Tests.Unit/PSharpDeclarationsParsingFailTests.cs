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

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class PSharpDeclarationsParsingFailTests
    {
        #region using declarations

        [TestMethod, Timeout(3000)]
        public void TestIncorrectUsingDeclaration()
        {
            var test = "using System.Text";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \";\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestUsingDeclarationWithoutIdentifier()
        {
            var test = "using;";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected identifier.",
                parser.GetParsingErrorLog());
        }

        #endregion

        #region namespace declarations

        [TestMethod, Timeout(3000)]
        public void TestUnexpectedTokenWithoutNamespace()
        {
            var test = "private";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Unexpected token.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestNamespaceDeclarationWithoutIdentifier()
        {
            var test = "namespace { }";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected namespace identifier.",
                parser.GetParsingErrorLog());
        }

        #endregion

        #region event declarations

        [TestMethod, Timeout(3000)]
        public void TestProtectedEventDeclaration()
        {
            var test = @"
namespace Foo {
protected event e;
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Event and machine declarations must be internal or public.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestPrivateEventDeclaration()
        {
            var test = @"
namespace Foo {
private event e;
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Event and machine declarations must be internal or public.");
        }

        [TestMethod, Timeout(3000)]
        public void TestEventDeclarationWithoutNamespace()
        {
            var test = "event e;";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Must be declared inside a namespace.",
                parser.GetParsingErrorLog());
        }

        #endregion

        #region machine declarations

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutState()
        {
            var test = @"
namespace Foo {
machine M {
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine must declare at least one state.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutIdentifier()
        {
            var test = @"
namespace Foo {
machine{}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected machine identifier.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithTwoBodies()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
}
{
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Unexpected token.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutStartState()
        {
            var test = @"
namespace Foo {
machine M {
state S { }
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine must declare a start state.",
                parser.GetParsingErrorLog());
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

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine must declare a start state.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutStartState3()
        {
            var test = @"
namespace Foo {
machine M {
state S1 { }
state S2 {}

state S3 { }
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine must declare a start state.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithMoreThanOneStartState()
        {
            var test = @"
namespace Foo {
machine M {
start state S1 {}
start state S2 { }

start state S3 { }
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine can declare only a single start state.",
                parser.GetParsingErrorLog());
        }

        #endregion

        #region state declarations

        [TestMethod, Timeout(3000)]
        public void TestStateDeclarationWithMoreThanOneEntry()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry {}
entry{}
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Duplicate entry declaration.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestStateDeclarationWithMoreThanOneExit()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
exit{}
exit {}
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Duplicate exit declaration.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestEntryDeclarationWithUnexpectedIdentifier()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry Bar {}
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"{\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventGotoStateDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \";\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventGotoStateDeclarationWithoutState()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto;
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected state identifier.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDoActionDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do Bar
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \";\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDoActionDeclarationWithoutAction()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do;
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected action identifier.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestOnEventDeclarationWithoutHandler()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e;
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"do\", \"goto\" or \"push\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestIgnoreEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e1 e2;
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \",\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestDeferEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e1 e2;
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \",\".",
                parser.GetParsingErrorLog());
        }

        #endregion

        #region field declarations

        [TestMethod, Timeout(3000)]
        public void TestPublicFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
public int k;
start state S { }
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A field or method cannot be public.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestInternalFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
internal int k;
start state S { }
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A field or method cannot be internal.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestIntFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
int k
start state S { }
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"(\" or \";\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
machine N
start state S { }
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"(\" or \";\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestPrivateMachineFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
private machine N
start state S { }
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"(\" or \";\".",
                parser.GetParsingErrorLog());
        }

        #endregion
    }
}
