// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.ProgramAware
{
    public class ReflectionBasedHasherTests
    {
        [Fact(Timeout = 5000)]
        public void BasicEqualityTests()
        {
            T1 t0 = new T1(1, "abc", 2);
            T1 t0Equal = new T1(1, "abc", 2);

            T1 t0Inverted = new T1(2, "abc", 1);

            Assert.True(
                ReflectionBasedHasher.HashObject(t0) == ReflectionBasedHasher.HashObject(t0Equal),
                $"Two equal objects of same type and value did not match");

            Assert.False(
                ReflectionBasedHasher.HashObject(t0) == ReflectionBasedHasher.HashObject(t0Inverted),
                $"Two unequal objects of same type but permuted field-values matched");
        }

        [Fact(Timeout = 5000)]
        public void NestedObjectEqualityTest()
        {
            ContainsT1 t0 = new ContainsT1(new T1(1, "abc", 2), "xyz");
            ContainsT1 t0Equal = new ContainsT1(new T1(1, "abc", 2), "xyz");

            Assert.True(
                ReflectionBasedHasher.HashObject(t0) == ReflectionBasedHasher.HashObject(t0Equal),
                $"Two equal objects of same type and value did not match");
        }

        [Fact(Timeout = 5000)]
        public void DifferentTypesInequalityTest()
        {
            T1 t0 = new T1(1, "abc", 2);
            T1EvilTwin t1EvilTwin = new T1EvilTwin(1, "abc", 2);

            Assert.False(
                ReflectionBasedHasher.HashObject(t0) == ReflectionBasedHasher.HashObject(t1EvilTwin),
                $"Two objects of different types matched");
        }

        [Fact(Timeout = 5000)]
        public void MaskedFieldEquality()
        {
            MaskedT t0 = new MaskedT("abc", "def");
            MaskedT t1 = new MaskedT("abc", "xyz");

            Assert.True(
                ReflectionBasedHasher.HashObject(t0) == ReflectionBasedHasher.HashObject(t1),
                $"Two otherwise-equal objects of (with non-matching masked field) did not match");
        }

        [Fact(Timeout = 5000)]
        public void InheritedFieldsTest()
        {
            SubT1 t0 = new SubT1(0, 1, "abc", 2);
            SubT1 t1 = new SubT1(0, 3, "xyz", 4);

            Assert.False(
                ReflectionBasedHasher.HashObject(t0) == ReflectionBasedHasher.HashObject(t1),
                $"Two objects with differing inherited fields matched");

            HashSet<Type> ignoreTypes = new HashSet<Type> { typeof(T1) };

            Assert.True(
                ReflectionBasedHasher.HashObject(t0, ignoreTypes) == ReflectionBasedHasher.HashObject(t1, ignoreTypes),
                $"Two objects differing only in inherited fields did not match despite ignoring inherited fields");
        }

        internal class T1
        {
            private readonly int X;
            internal readonly string Y;
            public readonly int Z;

            internal T1(int x, string y, int z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }
        }

        internal class T1EvilTwin
        {
            private readonly int X;
            internal readonly string Y;
            public readonly int Z;

            internal T1EvilTwin(int x, string y, int z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }
        }

        internal class ContainsT1
        {
            private readonly T1 T1Instance;
            private readonly string S;

            public ContainsT1(T1 t1Instance, string s)
            {
                this.T1Instance = t1Instance;
                this.S = s;
            }
        }

        internal class MaskedT
        {
            internal string S1;
            [ExcludeFromFingerprint]
            public string S2;

            public MaskedT(string s1, string s2)
            {
                this.S1 = s1;
                this.S2 = s2;
            }
        }

        internal class SubT1 : T1
        {
            private readonly int SubX;

            public SubT1(int subX, int x, string y, int z)
                : base(x, y, z)
            {
                this.SubX = subX;
            }
        }
    }
}
