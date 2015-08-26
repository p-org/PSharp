//-----------------------------------------------------------------------
// <copyright file="StateFailTests.cs" company="Microsoft">
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
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class StateFailTests
    {
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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

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

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \",\".",
                parser.GetParsingErrorLog());
        }
    }
}
