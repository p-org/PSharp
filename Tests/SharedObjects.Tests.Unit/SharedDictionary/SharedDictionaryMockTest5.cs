//-----------------------------------------------------------------------
// <copyright file="SharedDictionaryMockTest5.cs">
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

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedDictionaryMockTest5 : BaseTest
    {
        class E1 : Event
        {
            public bool flag;

            public E1(bool flag)
            {
                this.flag = flag;
            }
        }

        class E2 : Event
        {
            public ISharedDictionary<int, string> counter;
            public bool flag;

            public E2(ISharedDictionary<int, string> counter, bool flag)
            {
                this.counter = counter;
                this.flag = flag;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var flag = (this.ReceivedEvent as E1).flag;

                var counter = SharedDictionary.Create<int, string>(this.Id.RuntimeProxy);
                counter.TryAdd(1, "M");

                if (flag)
                {
                    this.CreateMachine(typeof(N), new E2(counter, false));
                }

                string v;
                var b = counter.TryGetValue(2, out v);

                if (!flag)
                {
                    this.Assert(!b);
                }

                if (b)
                {
                    this.Assert(v == "N");
                }
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var counter = (this.ReceivedEvent as E2).counter;

                bool b;
                b = counter.TryGetValue(1, out string v);
                this.Assert(b);
                this.Assert(v == "M");

                counter.TryAdd(2, "N");
            }
        }

        [Fact]
        public void TestDictionarySuccess1()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);

            var test = new Action<IMachineRuntime>((r) => {
                r.CreateMachine(typeof(M), new E1(true));
            });

            base.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestDictionarySuccess2()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);

            var test = new Action<IMachineRuntime>((r) => {
                r.CreateMachine(typeof(M), new E1(false));
            });

            base.AssertSucceeded(config, test);
        }
    }
}
