//-----------------------------------------------------------------------
// <copyright file="PSeqType.cs">
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

namespace Microsoft.PSharp.Parsing.PSyntax
{
    /// <summary>
    /// Seq type.
    /// </summary>
    internal sealed class PSeqType : PBaseType
    {
        #region fields

        /// <summary>
        /// The seq type.
        /// </summary>
        public PBaseType SeqType;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PSeqType()
        {
            base.Type = PType.Seq;
        }

        #endregion
    }
}
