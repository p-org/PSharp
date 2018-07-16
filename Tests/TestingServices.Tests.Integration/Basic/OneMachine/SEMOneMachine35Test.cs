//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine35Test.cs">
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
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine35Test : BaseTest
    {
        class Entry : Machine
        {
            List<int> rev;
            List<int> sorted;
            int i;
            int t;
            int s;
            bool swapped;
            bool b;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                rev = new List<int>();
                sorted = new List<int>();

                i = 0;
                while (i < 10)
                {
                    rev.Insert(0, i);
                    sorted.Add(i);
                    i = i + 1;
                }

                this.Assert(rev.Count == 10);

                // Assert that simply reversing the list produces a sorted list
                sorted = Reverse(rev);
                this.Assert(sorted.Count == 10);
                b = IsSorted(sorted);
                this.Assert(b);
                b = IsSorted(rev);
                this.Assert(!b);

                // Assert that BubbleSort returns the sorted list 
                sorted = BubbleSort(rev);
                this.Assert(sorted.Count == 10);
                b = IsSorted(sorted);
                this.Assert(b);
                b = IsSorted(rev);
                this.Assert(!b);
            }

            List<int> Reverse(List<int> l)
            {
                var result = l.ToList();

                i = 0;
                s = result.Count;
                while (i < s)
                {
                    t = result[i];
                    result.RemoveAt(i);
                    result.Insert(0, t);
                    i = i + 1;
                }

                return result;
            }

            List<int> BubbleSort(List<int> l)
            {
                var result = l.ToList();

                swapped = true;
                while (swapped)
                {
                    i = 0;
                    swapped = false;
                    while (i < result.Count - 1)
                    {
                        if (result[i] > result[i + 1])
                        {
                            t = result[i];
                            result[i] = result[i + 1];
                            result[i + 1] = t;
                            swapped = true;
                        }

                        i = i + 1;
                    }
                }

                return result;
            }

            bool IsSorted(List<int> l)
            {
                i = 0;
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
        public void TestSEMOneMachine35()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Entry));
            });

            base.AssertSucceeded(test);
        }
    }
}
