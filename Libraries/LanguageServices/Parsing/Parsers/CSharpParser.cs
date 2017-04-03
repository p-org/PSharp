//-----------------------------------------------------------------------
// <copyright file="CSharpParser.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Parsing.Framework;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The C# parser.
    /// </summary>
    public sealed class CSharpParser : BaseParser
    {
        #region fields

        /// <summary>
        /// The error log.
        /// </summary>
        private List<Tuple<SyntaxToken, string>> ErrorLog;

        /// <summary>
        /// The warning log.
        /// </summary>
        private List<Tuple<SyntaxToken, string>> WarningLog;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">ParsingOptions</param>
        public CSharpParser(ParsingOptions options)
            : base(options)
        {
            this.ErrorLog = new List<Tuple<SyntaxToken, string>>();
            this.WarningLog = new List<Tuple<SyntaxToken, string>>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="options">ParsingOptions</param>
        internal CSharpParser(PSharpProject project, SyntaxTree tree, ParsingOptions options)
            : base(project, tree, options)
        {
            this.ErrorLog = new List<Tuple<SyntaxToken, string>>();
            this.WarningLog = new List<Tuple<SyntaxToken, string>>();
        }

        /// <summary>
        /// Returns a P# program.
        /// </summary>
        /// <returns>P# program</returns>
        public IPSharpProgram Parse()
        {
            this.Program = this.CreateNewProgram();

            if (!base.Options.SkipErrorChecking)
            {
                this.ParseSyntaxTree();
            }

            this.CheckForErrorsAndWarnings();

            return this.Program;
        }

        /// <summary>
        /// Returns the parsing warning log.
        /// </summary>
        /// <returns>Parsing warning log</returns>
        public List<Tuple<SyntaxToken, string>> GetParsingWarningLog()
        {
            return this.WarningLog;
        }

        /// <summary>
        /// Returns the parsing error log.
        /// </summary>
        /// <returns>Parsing error log</returns>
        public List<Tuple<SyntaxToken, string>> GetParsingErrorLog()
        {
            return this.ErrorLog;
        }

        #endregion

        #region protected API

        /// <summary>
        /// Returns a new C# program.
        /// </summary>
        /// <returns>P# program</returns>
        protected override IPSharpProgram CreateNewProgram()
        {
            return new CSharpProgram(base.Project, base.SyntaxTree);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Parses the syntax tree for errors.
        /// </summary>
        private void ParseSyntaxTree()
        {
            new MachineDeclarationParser(base.Project, this.ErrorLog, this.WarningLog).
                Parse(base.SyntaxTree);
            new MonitorDeclarationParser(base.Project, this.ErrorLog, this.WarningLog).
                Parse(base.SyntaxTree);
            new MachineStateDeclarationParser(base.Project, this.ErrorLog, this.WarningLog).
                Parse(base.SyntaxTree);
            new MonitorStateDeclarationParser(base.Project, this.ErrorLog, this.WarningLog).
                Parse(base.SyntaxTree);
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
        /// <param name="reports">Reports</param>
        private void ReportParsingWarnings(List<string> reports)
        {
            if (!base.Options.ExitOnError || !base.Options.ShowWarnings)
            {
                return;
            }

            foreach (var warning in this.WarningLog)
            {
                var report = warning.Item2;
                var warningLine = base.SyntaxTree.GetLineSpan(warning.Item1.Span).StartLinePosition.Line + 1;

                var root = base.SyntaxTree.GetRoot();
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
        /// <param name="reports">Reports</param>
        private void ReportParsingErrors(List<string> reports)
        {
            if (!base.Options.ExitOnError)
            {
                return;
            }

            foreach (var error in this.ErrorLog)
            {
                var report = error.Item2;
                var errorLine = base.SyntaxTree.GetLineSpan(error.Item1.Span).StartLinePosition.Line + 1;
                
                var root = base.SyntaxTree.GetRoot();
                var lines = System.Text.RegularExpressions.Regex.Split(root.ToFullString(), "\r\n|\r|\n");

                report += "\nIn " + this.SyntaxTree.FilePath + " (line " + errorLine + "):\n";
                report += " " + lines[errorLine - 1];

                reports.Add(report);
            }
        }

        #endregion
    }
}
