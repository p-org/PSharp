// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

#if NET46
using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.PSharp.TestingServices.Coverage;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Provider for known types. Used for serialization.
    /// </summary>
    internal static class KnownTypesProvider
    {
        /// <summary>
        /// Known types used for serialization.
        /// </summary>
        public static List<Type> KnownTypes = new List<Type>
        {
            typeof(TestReport),
            typeof(CoverageInfo),
            typeof(Transition)
        };

        public static IEnumerable<Type> GetKnownTypes() => KnownTypes;
    }
}
#endif
