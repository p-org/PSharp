//-----------------------------------------------------------------------
// <copyright file="KnownTypesProvider.cs" company="Microsoft">
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
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// Provider for known types. Used for serialization.
    /// </summary>
    internal static class KnownTypesProvider
    {
        /// <summary>
        /// Known types used for serialization.
        /// </summary>
        public static List<Type> KnownTypes = new List<Type> {
            typeof(MachineId),
            typeof(Default),
            typeof(Halt)
        };

        public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
        {
            return KnownTypes;
        }
    }
}
