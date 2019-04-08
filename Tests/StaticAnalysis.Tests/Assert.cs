// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    internal static class Assert
    {
        internal static void Succeeded(string test, bool isPSharpProgram = true)
        {
            var configuration = Setup.GetConfiguration();
            Succeeded(configuration, test, isPSharpProgram);
        }

        internal static void Succeeded(Configuration configuration, string test, bool isPSharpProgram = true)
        {
            InMemoryLogger logger = new InMemoryLogger(true);

            try
            {
                var context = CompileTest(configuration, test, isPSharpProgram);
                var engine = StaticAnalysisEngine.Create(context, logger).Run();
                var numErrors = engine.ErrorReporter.ErrorCount;
                var numWarnings = engine.ErrorReporter.WarningCount;
                Xunit.Assert.Equal(0, numErrors);
                Xunit.Assert.Equal(0, numWarnings);
            }
            finally
            {
                logger.Dispose();
            }
        }

        internal static void Failed(string test, int numExpectedErrors, bool isPSharpProgram = true)
        {
            var configuration = Setup.GetConfiguration();
            Failed(configuration, test, numExpectedErrors, isPSharpProgram);
        }

        internal static void Failed(string test, int numExpectedErrors, string expectedOutput, bool isPSharpProgram = true)
        {
            var configuration = Setup.GetConfiguration();
            Failed(configuration, test, numExpectedErrors, expectedOutput, isPSharpProgram);
        }

        internal static void Failed(Configuration configuration, string test, int numExpectedErrors, bool isPSharpProgram = true)
        {
            Failed(configuration, test, numExpectedErrors, string.Empty, isPSharpProgram);
        }

        internal static void Failed(Configuration configuration, string test, int numExpectedErrors, string expectedOutput, bool isPSharpProgram = true)
        {
            InMemoryLogger logger = new InMemoryLogger(true);

            try
            {
                var context = CompileTest(configuration, test, isPSharpProgram);
                var engine = StaticAnalysisEngine.Create(context, logger).Run();

                var numErrors = engine.ErrorReporter.ErrorCount;
                Xunit.Assert.Equal(numExpectedErrors, numErrors);

                if (!string.IsNullOrEmpty(expectedOutput))
                {
                    var actual = logger.ToString();
                    Xunit.Assert.Equal(
                        expectedOutput.Replace(Environment.NewLine, string.Empty),
                        actual.Substring(0, actual.IndexOf(Environment.NewLine)));
                }
            }
            finally
            {
                logger.Dispose();
            }
        }

        internal static void Warning(string test, int numExpectedWarnings, bool isPSharpProgram = true)
        {
            var configuration = Setup.GetConfiguration();
            Warning(configuration, test, numExpectedWarnings, isPSharpProgram);
        }

        internal static void Warning(string test, int numExpectedWarnings, string expectedOutput, bool isPSharpProgram = true)
        {
            var configuration = Setup.GetConfiguration();
            Warning(configuration, test, numExpectedWarnings, expectedOutput, isPSharpProgram);
        }

        internal static void Warning(Configuration configuration, string test, int numExpectedWarnings, bool isPSharpProgram = true)
        {
            Warning(configuration, test, numExpectedWarnings, string.Empty, isPSharpProgram);
        }

        internal static void Warning(Configuration configuration, string test, int numExpectedWarnings, string expectedOutput, bool isPSharpProgram = true)
        {
            FailedAndWarning(configuration, test, 0, numExpectedWarnings, expectedOutput, isPSharpProgram);
        }

        internal static void FailedAndWarning(string test, int numExpectedErrors, int numExpectedWarnings, bool isPSharpProgram = true)
        {
            var configuration = Setup.GetConfiguration();
            FailedAndWarning(configuration, test, numExpectedErrors, numExpectedWarnings, isPSharpProgram);
        }

        internal static void FailedAndWarning(string test, int numExpectedErrors, int numExpectedWarnings, string expectedOutput, bool isPSharpProgram = true)
        {
            var configuration = Setup.GetConfiguration();
            FailedAndWarning(configuration, test, numExpectedErrors, numExpectedWarnings, expectedOutput, isPSharpProgram);
        }

        internal static void FailedAndWarning(Configuration configuration, string test, int numExpectedErrors, int numExpectedWarnings, bool isPSharpProgram = true)
        {
            FailedAndWarning(configuration, test, numExpectedErrors, numExpectedWarnings, string.Empty, isPSharpProgram);
        }

        internal static void FailedAndWarning(Configuration configuration, string test, int numExpectedErrors, int numExpectedWarnings, string expectedOutput, bool isPSharpProgram = true)
        {
            InMemoryLogger logger = new InMemoryLogger(true);
            configuration.ShowWarnings = true;

            try
            {
                var context = CompileTest(configuration, test, isPSharpProgram);
                var engine = StaticAnalysisEngine.Create(context, logger).Run();

                var numErrors = engine.ErrorReporter.ErrorCount;
                var numWarnings = engine.ErrorReporter.WarningCount;
                Xunit.Assert.Equal(numExpectedErrors, numErrors);
                Xunit.Assert.Equal(numExpectedWarnings, numWarnings);

                if (!string.IsNullOrEmpty(expectedOutput))
                {
                    var actual = logger.ToString();
                    Xunit.Assert.Equal(
                        expectedOutput.Replace(Environment.NewLine, string.Empty),
                        actual.Replace(Environment.NewLine, string.Empty));
                }
            }
            finally
            {
                logger.Dispose();
            }
        }

        private static CompilationContext CompileTest(Configuration configuration, string test, bool isPSharpProgram)
        {
            var context = CompilationContext.Create(configuration).LoadSolution(test, isPSharpProgram ? "psharp" : "cs");
            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();
            return context;
        }
    }
}
