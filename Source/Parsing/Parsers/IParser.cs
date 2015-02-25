//-----------------------------------------------------------------------
// <copyright file="IParser.cs">
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
using Microsoft.PSharp.Parsing.Syntax;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// Interface for a parser.
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Returns the parsed tokens.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        /// <returns>The P# program root</returns>
        ProgramRoot ParseTokens(List<Token> tokens);

        /// <summary>
        /// Returns the expected token types at the end of parsing.
        /// </summary>
        /// <returns>Expected token types</returns>
        List<TokenType> GetExpectedTokenTypes();
    }
}
