//-----------------------------------------------------------------------
// <copyright file="PSharpVisualStudioLexer.cs">
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

using System.Collections.Generic;

using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# Visual Studio lexer.
    /// </summary>
    public class PSharpVisualStudioLexer : PSharpLexer, IScanner
    {
        #region fields

        /// <summary>
        /// The visual studio buffer.
        /// </summary>
        private IVsTextBuffer Buffer;

        /// <summary>
        /// The current index.
        /// </summary>
        private int CurrentIndex;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        public PSharpVisualStudioLexer(IVsTextBuffer buffer)
            : base ()
        {
            this.Buffer = buffer;
            this.CurrentIndex = 0;
        }

        #endregion

        #region IScanner interface API

        /// <summary>
        /// Sets the source.
        /// </summary>
        /// <param name="source">String</param>
        /// <param name="offset">Int</param>
        void IScanner.SetSource(string source, int offset)
        {
            this.Tokens = new List<Token>();
            this.CurrentIndex = 0;

            var text = source.Substring(offset);
            var split = this.SplitText(text);
            foreach (var tok in split)
            {
                if (tok.Equals(""))
                {
                    continue;
                }

                base.TextUnits.Add(new TextUnit(tok));
            }

            base.TextUnits.Add(new TextUnit("\n"));

            this.TokenizeNext();
        }

        /// <summary>
        /// Scanes a token and provides information about it.
        /// </summary>
        /// <param name="tokenInfo">TokenInfo</param>
        /// <param name="state">Int</param>
        /// <returns></returns>
        bool IScanner.ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state)
        {
            bool foundToken = false;

            var token = this.GetNextToken();
            if (token != null)
            {
                if (token.Type == TokenType.MachineDecl ||
                    token.Type == TokenType.StateDecl ||
                    token.Type == TokenType.EventDecl ||
                    token.Type == TokenType.ActionDecl)
                {
                    tokenInfo.Type = VisualStudio.Package.TokenType.Keyword;
                    tokenInfo.Color = TokenColor.Keyword;
                }
            }

            tokenInfo.Type = VisualStudio.Package.TokenType.Keyword;
            tokenInfo.Color = TokenColor.Keyword;
            return true;

            return foundToken;
        }

        #endregion

        #region private API

        private Token GetNextToken()
        {
            Token token = null;
            //if (this.CurrentIndex < base.Tokens.Count)
            //{
            //    token = base.Tokens[this.CurrentIndex];
            //    this.CurrentIndex++;
            //}
            
            return token;
        }

        #endregion
    }
}
