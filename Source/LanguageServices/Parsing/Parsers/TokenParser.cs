﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
        #region fields

        /// <summary>
        /// List of original tokens.
        /// </summary>
        protected List<Token> OriginalTokens;

        /// <summary>
        /// The token stream.
        /// </summary>
        protected TokenStream TokenStream;

        /// <summary>
        /// List of expected token types at end of parsing.
        /// </summary>
        protected List<TokenType> ExpectedTokenTypes;

        /// <summary>
        /// The error log.
        /// </summary>
        protected StringBuilder ErrorLog;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">ParsingOptions</param>
        public TokenParser(ParsingOptions options)
            : base(options)
        {
            this.ErrorLog = new StringBuilder();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="options">ParsingOptions</param>
        internal TokenParser(PSharpProject project, SyntaxTree tree, ParsingOptions options)
            : base(project, tree, options)
        {
            this.ErrorLog = new StringBuilder();
        }

        /// <summary>
        /// Returns a P# program.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        /// <returns>P# program</returns>
        public IPSharpProgram ParseTokens(List<Token> tokens)
        {
            this.OriginalTokens = tokens.ToList();
            this.TokenStream = new TokenStream(tokens);
            this.Program = this.CreateNewProgram();
            this.ExpectedTokenTypes = new List<TokenType>();

            try
            {
                this.ParseTokens();
            }
            catch (ParsingException ex)
            {
                this.ErrorLog.Append(ex.Message);
                this.ExpectedTokenTypes = ex.ExpectedTokenTypes;
                this.ReportParsingError();

                if (base.Options.ThrowParsingException &&
                    this.ErrorLog.Length > 0)
                {
                    throw new ParsingException(this.ErrorLog.ToString(), ex.ExpectedTokenTypes, ex);
                }
            }
            
            return this.Program;
        }

        /// <summary>
        /// Returns the expected token types at the end of parsing.
        /// </summary>
        /// <returns>Expected token types</returns>
        public List<TokenType> GetExpectedTokenTypes()
        {
            return this.ExpectedTokenTypes;
        }

        /// <summary>
        /// Returns the parsing error log.
        /// </summary>
        /// <returns>Parsing error log</returns>
        public string GetParsingErrorLog()
        {
            return this.ErrorLog.ToString();
        }

        #endregion

        #region protected API

        /// <summary>
        /// Parses the tokens.
        /// </summary>
        protected abstract void ParseTokens();

        /// <summary>
        /// Reports a parsing error.
        /// </summary>
        protected void ReportParsingError()
        {
            if ((!base.Options.ExitOnError &&
                !base.Options.ThrowParsingException) ||
                this.ErrorLog.Length == 0)
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

            if (base.Options.ExitOnError)
            {
                Error.ReportAndExit(this.ErrorLog.ToString());
            }
        }

        #endregion
    }
}
