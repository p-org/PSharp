﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class BubbleSortAlgorithmTest : BaseTest
    {
        public BubbleSortAlgorithmTest(ITestOutputHelper output)
            : base(output)
        { }

        class BubbleSortMachine : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var rev = new List<int>();
                var sorted = new List<int>();

                for (int i = 0; i < 10; i++)
                {
                    rev.Insert(0, i);
                    sorted.Add(i);
                }

                this.Assert(rev.Count == 10);

                // Assert that simply reversing the list produces a sorted list.
                sorted = Reverse(rev);
                this.Assert(sorted.Count == 10);
                this.Assert(this.IsSorted(sorted));
                this.Assert(!this.IsSorted(rev));

                // Assert that the algorithm returns the sorted list.
                sorted = this.Sort(rev);
                this.Assert(sorted.Count == 10);
                this.Assert(this.IsSorted(sorted));
                this.Assert(!this.IsSorted(rev));
            }

            List<int> Reverse(List<int> l)
            {
                var result = l.ToList();

                int i = 0;
                int s = result.Count;
                while (i < s)
                {
                    int temp = result[i];
                    result.RemoveAt(i);
                    result.Insert(0, temp);
                    i = i + 1;
                }

                return result;
            }

            List<int> Sort(List<int> l)
            {
                var result = l.ToList();

                var swapped = true;
                while (swapped)
                {
                    int i = 0;
                    swapped = false;
                    while (i < result.Count - 1)
                    {
                        if (result[i] > result[i + 1])
                        {
                            int temp = result[i];
                            result[i] = result[i + 1];
                            result[i + 1] = temp;
                            swapped = true;
                        }

                        i = i + 1;
                    }
                }

                return result;
            }

            bool IsSorted(List<int> l)
            {
                int i = 0;
                while (i < l.Count - 1)
                {
                    if (l[i] > l[i + 1])
                    {
                        return false;
                    }

                    i = i + 1;
                }

                return true;
            }
        }

        [Fact]
        public void TestBubbleSortAlgorithm()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(BubbleSortMachine));
            });

            base.AssertSucceeded(test);
        }
    }
}
