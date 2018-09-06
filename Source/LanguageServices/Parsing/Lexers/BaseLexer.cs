// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// An abstract lexer.
    /// </summary>
    public abstract class BaseLexer : ILexer
    {
        #region fields

        /// <summary>
        /// List of tokens.
        /// </summary>
        protected List<Token> Tokens;

        /// <summary>
        /// List of text units to be tokenized.
        /// </summary>
        protected List<TextUnit> TextUnits;

        /// <summary>
        /// The current index.
        /// </summary>
        protected int Index;

        #endregion

        #region public API

        /// <summary>
        /// Tokenizes the given text.
        /// </summary>
        /// <param name="text">Text to tokenize</param>
        /// <returns>List of tokens</returns>
        public List<Token> Tokenize(string text)
        {
            if (text.Length == 0)
            {
                return new List<Token>();
            }

            this.Tokens = new List<Token>();
            this.TextUnits = new List<TextUnit>();
            this.Index = 0;

            using (StringReader sr = new StringReader(text))
            {
                int position = 0;
                int line = 1;
                string lineText;
                while ((lineText = sr.ReadLine()) != null)
                {
                    var split = this.SplitText(lineText);
                    foreach (var tok in split)
                    {
                        if (tok.Equals(""))
                        {
                            continue;
                        }

                        this.TextUnits.Add(new TextUnit(tok, line));
                        position += tok.Length;
                    }

                    this.TextUnits.Add(new TextUnit("\n", line));
                    position++;
                    line++;
                }
            }
            
            while (this.Index < this.TextUnits.Count)
            {
                this.TokenizeNext();
            }

            return this.Tokens;
        }

        #endregion

        #region protected API

        /// <summary>
        /// Splits the given text using a regex pattern and returns the split text.
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Tokenized text</returns>
        protected string[] SplitText(string text)
        {
            return Regex.Split(text, this.GetPattern());
        }

        /// <summary>
        /// Tokenizes the next text unit.
        /// </summary>
        protected abstract void TokenizeNext();

        /// <summary>
        /// Returns the regex pattern.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetPattern();

        #endregion
    }
}
