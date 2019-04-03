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

        /// <summary>
        /// Tokenizes the given text.
        /// </summary>
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
                    foreach (var token in split)
                    {
                        if (string.IsNullOrEmpty(token))
                        {
                            continue;
                        }

                        this.TextUnits.Add(new TextUnit(token, line));
                        position += token.Length;
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

        /// <summary>
        /// Splits the given text using a regex pattern and returns the split text.
        /// </summary>
        protected string[] SplitText(string text) => Regex.Split(text, this.GetPattern());

        /// <summary>
        /// Tokenizes the next text unit.
        /// </summary>
        protected abstract void TokenizeNext();

        /// <summary>
        /// Returns the regex pattern.
        /// </summary>
        protected abstract string GetPattern();
    }
}
