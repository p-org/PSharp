//-----------------------------------------------------------------------
// <copyright file="EventTests.cs" company="Microsoft">
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
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class EventTests
    {
        [TestMethod, Timeout(3000)]
        public void TestEventDeclaration()
        {
            var test = @"
namespace Foo {
event e1;
internal event e2;
public event e3;
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var parserConfig = new LanguageServicesConfiguration();
            var program = new PSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using System;
using Microsoft.PSharp;
namespace Foo
{
class e1 : Event
{
 internal e1()
  : base()
 { }
}
internal class e2 : Event
{
 internal e2()
  : base()
 { }
}
public class e3 : Event
{
 internal e3()
  : base()
 { }
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }
    }
}
