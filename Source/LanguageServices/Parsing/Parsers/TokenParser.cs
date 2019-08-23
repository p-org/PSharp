// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// An abstract token parser.
    /// </summary>
    public abstract class TokenParser : BaseParser, IParser
    {
        /// <summary>
        /// List of original tokens.
        /// </summary>
        protected List<Token> OriginalTokens;

        /// <summary>
        /// The token stream.
        /// </summary>
        protected TokenStream TokenStream;

        /// <summary>
        /// The expected token types at the end of parsing.
        /// </summary>
        protected TokenType[] ExpectedTokenTypes;

        /// <summary>
        /// The error log.
        /// </summary>
        protected StringBuilder ErrorLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenParser"/> class.
        /// </summary>
        public TokenParser(ParsingOptions options)
            : base(options) =>
            this.ErrorLog = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenParser"/> class.
        /// </summary>
        internal TokenParser(PSharpProject project, SyntaxTree tree, ParsingOptions options)
            : base(project, tree, options) =>
            this.ErrorLog = new StringBuilder();

        /// <summary>
        /// Returns a P# program.
        /// </summary>
        public IPSharpProgram ParseTokens(List<Token> tokens)
        {
            this.OriginalTokens = tokens.ToList();
            this.TokenStream = new TokenStream(tokens);
            this.Program = this.CreateNewProgram();
            this.ExpectedTokenTypes = Array.Empty<TokenType>();

            try
            {
                this.ParseTokens();
            }
            catch (ParsingException ex)
            {
                this.ExpectedTokenTypes = ex.ExpectedTokenTypes;
                if (this.Options.IsParsingForVsLanguageService)
                {
                    // Rethrow the exception to the language service to be processed for its error info.
                    throw;
                }

                this.ErrorLog.Append(ex.Message);
                this.ReportParsingError();

                if (this.Options.ThrowParsingException && this.ErrorLog.Length > 0)
                {
                    throw new ParsingException(this.ErrorLog.ToString(), ex, ex.FailingToken, ex.ExpectedTokenTypes);
                }
            }

            return this.Program;
        }

        /// <summary>
        /// Returns the expected token types at the end of parsing.
        /// </summary>
        public TokenType[] GetExpectedTokenTypes() => this.ExpectedTokenTypes;

        /// <summary>
        /// Returns the parsing error log.
        /// </summary>
        public string GetParsingErrorLog() => this.ErrorLog.ToString();

        /// <summary>
        /// Parses the tokens.
        /// </summary>
        protected abstract void ParseTokens();

        /// <summary>
        /// Reports a parsing error.
        /// </summary>
        protected void ReportParsingError()
        {
            if ((!this.Options.ExitOnError && !this.Options.ThrowParsingException)
                || this.ErrorLog.Length == 0)
            {
                return;
            }

            var errorIndex = this.TokenStream.Index;
            if (this.TokenStream.Index == this.TokenStream.Length &&
                this.TokenStream.Index > 0)
            {
                errorIndex--;
            }

            var errorToken = this.TokenStream.GetAt(errorIndex);
            var errorLine = this.OriginalTokens.Where(
                val => val.TextUnit.Line == errorToken.TextUnit.Line).ToList();

            this.ErrorLog.Append($"\nIn {this.SyntaxTree.FilePath} (line {errorToken.TextUnit.Line}):\n");

            int nonWhiteIndex = 0;
            for (int idx = 0; idx < errorLine.Count; idx++)
            {
                if (errorLine[idx].Type != TokenType.WhiteSpace)
                {
                    nonWhiteIndex = idx;
                    break;
                }
            }

            for (int idx = nonWhiteIndex; idx < errorLine.Count; idx++)
            {
                this.ErrorLog.Append(string.Format("{0}", errorLine[idx].TextUnit.Text));
            }

            for (int idx = nonWhiteIndex; idx < errorLine.Count; idx++)
            {
                if (errorLine[idx].Equals(errorToken) && errorIndex == this.TokenStream.Index)
                {
                    this.ErrorLog.Append('^', errorLine[idx].TextUnit.Text.Length);
                    break;
                }
                else
                {
                    this.ErrorLog.Append('_', errorLine[idx].TextUnit.Text.Length);
                }
            }

            if (errorIndex != this.TokenStream.Index)
            {
                this.ErrorLog.Append("^");
            }

            if (this.Options.ExitOnError)
            {
                Error.ReportAndExit(this.ErrorLog.ToString());
            }
        }
    }
}
