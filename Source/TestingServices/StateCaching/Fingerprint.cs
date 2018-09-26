// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
