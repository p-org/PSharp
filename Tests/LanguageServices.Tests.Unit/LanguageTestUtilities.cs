// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    internal static class LanguageTestUtilities
    {
        internal static void AssertRewritten(string expectedResult, string test, bool isPSharpProgram = true, Version csVersion = null)
        {
            CompilationContext context = RunRewriter(test, isPSharpProgram, csVersion);
            expectedResult = expectedResult.TrimStart();    // The tests create expectedResults with a leading crlf

            var project = context.GetProjects()[0];
            var syntaxTree = isPSharpProgram
                    ? project.PSharpPrograms[0].GetSyntaxTree()
                    : project.CSharpPrograms[0].GetSyntaxTree();
            var actualResult = syntaxTree.ToString();

            // Rewriting appends a newline only "\n" for PSharp and "\r\n" for CSharp; 'expected' has "\r\n". Keep blank lines.
            var expectedLines = expectedResult.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var actualLines = actualResult.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var numLines = Math.Min(expectedLines.Length, actualLines.Length);
            for (var ii = 0; ii < numLines; ++ii)
            {
                var expected = expectedLines[ii];
                var actual = actualLines[ii];

                if (expected != actual)
                {
                    // Use same number of characters for as close alignment as possible for non-fixed fonts.
                    var message = string.Format("Line {0}:{1}expect: {2}{3}actual: {4}",
                        ii + 1, Environment.NewLine, expected, Environment.NewLine, actual);
                    Assert.True(false, message);
                }
            }

            if (expectedLines.Length != actualLines.Length)
            {
                // Ignore one extra trailing crlf from actual (some of the tests may have forgotten to add it to expected).
                if ((actualLines.Length != expectedLines.Length + 1) || (actualLines[actualLines.Length - 1] != string.Empty))
                {
                    var line = expectedLines.Length > actualLines.Length ? expectedLines[numLines] : actualLines[numLines];
                    var message = string.Format("{0} has more lines, starting at line {1}:{2}",
                        expectedLines.Length > actualLines.Length ? "expected" : "actual", numLines + 1,
                        line.Length > 0 ? line : "<empty>");
                    Assert.True(false, message);
                }
            }
        }

        internal static CompilationContext RunRewriter(string test, bool isPSharpProgram = true, Version csVersion = null)
        {
            var configuration = Configuration.Create();
            configuration.Verbose = 2;
            if (csVersion != null)
            {
                configuration.RewriteCSharpVersion = csVersion;
            }

            // There is some inconsistency around whether the rewriting process strips leading newlines, so make sure there are none.
            var context = CompilationContext.Create(configuration).LoadSolution(test.Trim(), isPSharpProgram ? "psharp" : "cs");

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();
            return context;
        }

        internal static void AssertFailedTestLog(string expectedResult, string test)
        {
            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.Equal(expectedResult, parser.GetParsingErrorLog());
        }
    }
}
