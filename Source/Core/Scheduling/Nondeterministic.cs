//-----------------------------------------------------------------------
// <copyright file="Nondeterministic.cs" company="Microsoft">
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
using System.Security.Cryptography;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Static class implementing nondeterministic values.
    /// </summary>
    internal static class Nondeterministic
    {
        /// <summary>
        /// Nondeterministic boolean choice.
        /// </summary>
        internal static bool Choice
        {
            get
            {
                return Nondeterministic.GetBoolean();
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean value.
        /// </summary>
        /// <returns>bool</returns>
        private static bool GetBoolean()
        {
            bool result = false;

            if (Nondeterministic.UnsignedInteger(2) == 1)
                result = true;

            return result;
        }

        /// <summary>
        /// Returns a non deterministic unsigned integer.
        /// The return value v is equal or greater than 0
        /// and lower than the given ceiling.
        /// </summary>
        /// <param name="ceiling">Ceiling</param>
        /// <returns>int</returns>
        private static int UnsignedInteger(int ceiling)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            byte[] buffer = new byte[4];
            int bc, val;

            if ((ceiling & -ceiling) == ceiling)
            {
                rng.GetBytes(buffer);
                bc = BitConverter.ToInt32(buffer, 0);
                return bc & (ceiling - 1);
            }

            do
            {
                rng.GetBytes(buffer);
                bc = BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF;
                val = bc % ceiling;
            } while (bc - val + (ceiling - 1) < 0);

            return val;
        }
    }
}
