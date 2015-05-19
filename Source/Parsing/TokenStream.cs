//-----------------------------------------------------------------------
// <copyright file="TokenStream.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
using System.Linq;

namespace Microsoft.PSharp.Parsing
{
    public sealed class TokenStream
    {
        #region fields

        /// <summary>
        /// List of tokens in the stream.
        /// </summary>
        private List<Token> Tokens;

        /// <summary>
        /// The current index of the stream.
        /// </summary>
        public int Index;

        /// <summary>
        /// The length of the stream.
        /// </summary>
        public int Length
        {
            get
            {
                return this.Tokens.Count;
            }
        }

        /// <summary>
        /// True if no tokens remaining in the stream.
        /// </summary>
        public bool Done
        {
            get
            {
                return this.Index == this.Length;
            }
        }

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public TokenStream(List<Token> tokens)
        {
            this.Tokens = tokens.ToList();
            this.Index = 0;
        }

        /// <summary>
        /// Returns the next token in the stream and progresses by one token.
        /// Returns null if the stream is empty.
        /// </summary>
        /// <returns>Token</returns>
        public Token Next()
        {
            if (this.Index == this.Tokens.Count)
            {
                return null;
            }

            var token = this.Tokens[this.Index];
            this.Index++;

            return token;
        }

        /// <summary>
        /// Consumes the next token in the stream. Does nothing 
        /// if the stream is empty.
        /// </summary>
        public void Consume()
        {
            if (this.Index == this.Tokens.Count)
            {
                return;
            }
            
            this.Tokens.RemoveAt(this.Index);
        }

        /// <summary>
        /// Returns the next token in the stream without progressing to the next token.
        /// Returns null if the stream is empty.
        /// </summary>
        /// <returns>Token</returns>
        public Token Peek()
        {
            if (this.Index == this.Tokens.Count)
            {
                return null;
            }
            
            return this.Tokens[this.Index];
        }

        /// <summary>
        /// Swaps the current token with the new token. Does nothing if the stream is
        /// empty.
        /// </summary>
        public void Swap(Token token)
        {
            if (this.Index == this.Tokens.Count)
            {
                return;
            }

            this.Tokens[this.Index] = token;
        }

        /// <summary>
        /// Returns the token in the given index of the stream. Returns
        /// null if the index is out of bounds.
        /// </summary>
        /// <returns>Token</returns>
        public Token GetAt(int index)
        {
            if (index >= this.Tokens.Count || index < 0)
            {
                return null;
            }
            
            return this.Tokens[index];
        }

        #endregion
    }
}
