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

namespace Microsoft.PSharp.TestingServices.StateCaching
{
    /// <summary>
    /// Class implementing a program state fingerprint.
    /// </summary>
    internal sealed class Fingerprint
    {
        /// <summary>
        /// The hash value of the fingerprint.
        /// </summary>
        private readonly int HashValue;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hash">HashValue</param>
        internal Fingerprint(int hash)
        {
            HashValue = hash;
        }

        /// <summary>
        /// Returns true if the fingerprint is equal to
        /// the given object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            var fingerprint = obj as Fingerprint;
            return fingerprint != null && HashValue == fingerprint.HashValue;
        }

        /// <summary>
        /// Returns the hashcode of the fingerprint.
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode()
        {
            return HashValue;
        }

        /// <summary>
        /// Returns a string representation of the fingerprint.
        /// </summary>
        /// <returns>Text</returns>
        public override string ToString()
        {
            return $"fingerprint['{HashValue}']";
        }
    }
}
