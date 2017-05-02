//-----------------------------------------------------------------------
// <copyright file="Fingerprint.cs">
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

using System;
using System.Globalization;

namespace Microsoft.PSharp.TestingServices.StateCaching
{
    /// <summary>
    /// Class implementing a program state fingerprint.
    /// </summary>
    internal sealed class Fingerprint
    {
        #region fields

        /// <summary>
        /// The hash value of the fingerprint.
        /// </summary>
        private int HashValue;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hash">HashValue</param>
        internal Fingerprint(int hash)
        {
            this.HashValue = hash;
        }

        #endregion

        #region public API

        /// <summary>
        /// Returns true if the fingerprint is equal to
        /// the given object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            var fingerprint = obj as Fingerprint;
            var result = false;

            if (fingerprint != null &&
                this.HashValue == fingerprint.HashValue)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Returns the hashcode of the fingerprint.
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode()
        {
            return this.HashValue;
        }

        /// <summary>
        /// Returns a string representation of the fingerprint.
        /// </summary>
        /// <returns>Text</returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture,
                "fingerprint['{0}']", this.HashValue);
        }

        #endregion
    }
}
