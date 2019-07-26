﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics.StepSignatures
{
    /// <summary>
    /// Uses reflection to come up with a hash of the objects based on the values of members.
    /// </summary>
    public static class ReflectionBasedHasher
    {
        private const int MOD = 1000000007;

        /// <summary>
        /// Uses reflection to come up with a hash of the objects based on the values of members.
        /// </summary>
        /// <param name="o">The object to hash. Must not be null</param>
        /// <returns>A hash of the object</returns>
        public static int HashObject(object o)
        {
            int objectHash = o.GetType().GetHashCode();

            if (o.GetType().GetMethod("GetHashCode").DeclaringType == typeof(object))
            {
                foreach (FieldInfo fieldInfo in o.GetType().GetFields())
                {
                    if (fieldInfo.GetCustomAttribute(typeof(ExcludeFromFingerprintAttribute)) != null)
                    {
                        continue;
                    }

                    object f = fieldInfo.GetValue(o);
                    int fieldHash = (f == null) ? fieldInfo.FieldType.GetHashCode() : HashObject(f);

                    objectHash = (objectHash << 1) * fieldHash;
                    objectHash %= MOD;
                }
            }
            else
            {
                objectHash = (objectHash << 1) * o.GetHashCode();
                objectHash %= MOD;
            }

            return objectHash;
        }
#if NOPE_NOPE_NOPE
        /// <summary>
        /// Attribute to mark that this field should be skipped while computing the hash for this object.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field, Inherited = false)]
        public sealed class ExcludeFromFingerprintAttribute : Attribute
        {
        }
#endif
    }
}
