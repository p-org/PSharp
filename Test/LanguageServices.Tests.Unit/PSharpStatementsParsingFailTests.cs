//-----------------------------------------------------------------------
// <copyright file="PSharpStatementsParsingFailTests.cs" company="Microsoft">
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
    public class PSharpStatementsParsingFailTests
    {
        #region create statements

        [TestMethod, Timeout(3000)]
        public void TestCreateStatementInStateWithoutBrackets()
        {
            var test = @"
namespace Foo {
machine M {
machine N;
start state S
{
entry
{
N = create N;
}
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"(\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestCreateStatementInStateWithoutIdentifier()
        {
            var test = @"
namespace Foo {
machine M {
machine N;
start state S
{
entry
{
N = create;
}
}
}
}";

            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected machine identifier.",
                parser.GetParsingErrorLog());
        }

        #endregion
    }
}
