//-----------------------------------------------------------------------
// <copyright file="PMapType.cs">
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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Map type.
    /// </summary>
    internal sealed class PMapType : PBaseType
    {
        #region fields

        /// <summary>
        /// The key type.
        /// </summary>
        public PBaseType KeyType;

        /// <summary>
        /// The value type.
        /// </summary>
        public PBaseType ValueType;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PMapType()
        {
            base.Type = PType.Map;
        }

        #endregion
    }
}
