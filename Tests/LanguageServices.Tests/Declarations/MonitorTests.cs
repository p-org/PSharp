using Microsoft.CodeAnalysis.CSharp;
using Microsoft.PSharp.LanguageServices.Parsing;

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class MonitorTests
    {
        [Fact(Timeout=5000)]
        public void TestPrivateMonitorDeclaration()
        {
            var test = @"
namespace Foo {
private monitor M { }
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.Equal("A monitor cannot be declared as private.", parser.GetParsingErrorLog());
        }

        [Fact(Timeout=5000)]
        public void TestProtectedMonitorDeclaration()
        {
            var test = @"
namespace Foo {
protected monitor M { }
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.Equal("A monitor cannot be declared as protected.", parser.GetParsingErrorLog());
        }
    }
}
