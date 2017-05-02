//-----------------------------------------------------------------------
// <copyright file="PSharpTokenTag.cs">
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

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# token tag.
    /// </summary>
    internal class PSharpTokenTag : ITag
    {
        public TokenType Type { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">TokenType</param>
        public PSharpTokenTag(TokenType type)
        {
            this.Type = type;
        }
    }
}
