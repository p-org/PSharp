//-----------------------------------------------------------------------
// <copyright file="Fingerprint.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.PSharp.StateCaching
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
        /// <returns>Boolean value</returns>
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
                "fingerprint: {0}", this.HashValue);
        }

        #endregion
    }
}
