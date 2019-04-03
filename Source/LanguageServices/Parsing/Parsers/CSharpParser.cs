// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.PSharp.LanguageServices.Parsing.Framework;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The C# parser.
    /// </summary>
    public sealed class CSharpParser : BaseParser
    {
        /// <summary>
        /// The error log.
        /// </summary>
        private readonly List<Tuple<SyntaxToken, string>> ErrorLog;

        /// <summary>
        /// The warning log.
        /// </summary>
        private readonly List<Tuple<SyntaxToken, string>> WarningLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpParser"/> class.
        /// </summary>
        public CSharpParser(ParsingOptions options)
            : base(options)
        {
            this.ErrorLog = new List<Tuple<SyntaxToken, string>>();
            this.WarningLog = new List<Tuple<SyntaxToken, string>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpParser"/> class.
        /// </summary>
        internal CSharpParser(PSharpProject project, SyntaxTree tree, ParsingOptions options)
            : base(project, tree, options)
        {
            this.ErrorLog = new List<Tuple<SyntaxToken, string>>();
            this.WarningLog = new List<Tuple<SyntaxToken, string>>();
        }

        /// <summary>
        /// Returns a P# program.
        /// </summary>
        public IPSharpProgram Parse()
        {
            this.Program = this.CreateNewProgram();

            if (!this.Options.SkipErrorChecking)
            {
                this.ParseSyntaxTree();
            }

            this.CheckForErrorsAndWarnings();

            return this.Program;
        }

        /// <summary>
        /// Returns the parsing warning log.
        /// </summary>
        public List<Tuple<SyntaxToken, string>> GetParsingWarningLog()
        {
            return this.WarningLog;
        }

        /// <summary>
        /// Returns the parsing error log.
        /// </summary>
        public List<Tuple<SyntaxToken, string>> GetParsingErrorLog()
        {
            return this.ErrorLog;
        }

        /// <summary>
        /// Returns a new C# program.
        /// </summary>
        protected override IPSharpProgram CreateNewProgram()
        {
            return new CSharpProgram(this.Project, this.SyntaxTree);
        }

        /// <summary>
        /// Parses the syntax tree for errors.
        /// </summary>
        private void ParseSyntaxTree()
        {
            new MachineDeclarationParser(this.Project, this.ErrorLog, this.WarningLog).
                Parse(this.SyntaxTree);
            new MonitorDeclarationParser(this.Project, this.ErrorLog, this.WarningLog).
                Parse(this.SyntaxTree);
            new MachineStateDeclarationParser(this.Project, this.ErrorLog, this.WarningLog).
                Parse(this.SyntaxTree);
            new MonitorStateDeclarationParser(this.Project, this.ErrorLog, this.WarningLog).
                Parse(this.SyntaxTree);
        }

        /// <summary>
        /// Checks for parsing errors and warnings. If any errors
        /// are found (or warnings, if warnings are enabled) then
        /// it throws an exception.
        /// </summary>
        private void CheckForErrorsAndWarnings()
        {
            var warnings = new List<string>();
            var errors = new List<string>();

            if (this.WarningLog.Count > 0)
            {
                this.ReportParsingWarnings(warnings);
            }

            if (this.ErrorLog.Count > 0)
            {
                this.ReportParsingErrors(errors);
            }

            if (errors.Count > 0 || warnings.Count > 0)
            {
                throw new ParsingException(errors, warnings);
            }
        }

        /// <summary>
        /// Reports the parsing warnings. Only works if the
        /// parser is running internally.
        /// </summary>
        private void ReportParsingWarnings(List<string> reports)
        {
            if (!this.Options.ExitOnError || !this.Options.ShowWarnings)
            {
                return;
            }

            foreach (var warning in this.WarningLog)
            {
                var report = warning.Item2;
                var warningLine = this.SyntaxTree.GetLineSpan(warning.Item1.Span).StartLinePosition.Line + 1;

                var root = this.SyntaxTree.GetRoot();
                var lines = System.Text.RegularExpressions.Regex.Split(root.ToFullString(), "\r\n|\r|\n");

                report += "\nIn " + this.SyntaxTree.FilePath + " (line " + warningLine + "):\n";
                report += " " + lines[warningLine - 1];

                reports.Add(report);
            }
        }

        /// <summary>
        /// Reports the parsing errors and exits. Only works if the
        /// parser is running internally.
        /// </summary>
        private void ReportParsingErrors(List<string> reports)
        {
            if (!this.Options.ExitOnError)
            {
                return;
            }

            foreach (var error in this.ErrorLog)
            {
                var report = error.Item2;
                var errorLine = this.SyntaxTree.GetLineSpan(error.Item1.Span).StartLinePosition.Line + 1;

                var root = this.SyntaxTree.GetRoot();
                var lines = System.Text.RegularExpressions.Regex.Split(root.ToFullString(), "\r\n|\r|\n");

                report += "\nIn " + this.SyntaxTree.FilePath + " (line " + errorLine + "):\n";
                report += " " + lines[errorLine - 1];

                reports.Add(report);
            }
        }
    }
}
