//-----------------------------------------------------------------------
// <copyright file="AnonymousActionHandler.cs">
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

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Anonymous action handler syntax node.
    /// </summary>
    internal class AnonymousActionHandler
    {
        #region fields

        /// <summary>
        /// The block containing the handler statements.
        /// </summary>
        internal readonly BlockSyntax BlockSyntax;

        /// <summary>
        /// Indicates whether the generated method should be 'async Task'.
        /// </summary>
        internal readonly bool IsAsync;

        #endregion

        #region internal API

        internal AnonymousActionHandler(BlockSyntax blockSyntax, bool isAsync)
        {
            this.BlockSyntax = blockSyntax;
            this.IsAsync = isAsync;
        }
        
        #endregion
    }
}
