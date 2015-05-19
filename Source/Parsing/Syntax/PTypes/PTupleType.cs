//-----------------------------------------------------------------------
// <copyright file="PTupleType.cs">
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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Tuple type.
    /// </summary>
    internal sealed class PTupleType : PBaseType
    {
        #region fields

        /// <summary>
        /// The tuple types.
        /// </summary>
        public List<PBaseType> TupleTypes;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PTupleType()
        {
            base.Type = PType.Tuple;
            this.TupleTypes = new List<PBaseType>();
        }

        #endregion
    }
}
