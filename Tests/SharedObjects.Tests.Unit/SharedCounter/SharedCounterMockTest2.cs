//-----------------------------------------------------------------------
// <copyright file="SharedCounterMockTest2.cs">
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
    public class SharedCounterMockTest2 : BaseTest
    {
        class Conf : Event
        {
            public int flag;
            public Conf(int flag)
            {
                this.flag = flag;
            }
        }

        class E : Event
        {
            public ISharedCounter counter;

            public E(ISharedCounter counter)
            {
                this.counter = counter;
            }
        }

        class Done : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var flag = (this.ReceivedEvent as Conf).flag;

                var counter = SharedCounter.Create(this.Id.GetRuntimeProxy(), 0);
                var n = this.CreateMachine(typeof(N), new E(counter));

                int v1 = counter.CompareExchange(10, 0); // if 0 then 10
                int v2 = counter.GetValue();

                if (flag == 0)
                {
                    this.Assert(
                        (v1 == 5 && v2 == 5) ||
                        (v1 == 0 && v2 == 10) ||
                        (v1 == 0 && v2 == 15)
                        );
                }
                else if (flag == 1)
                {
                    this.Assert(
                        (v1 == 0 && v2 == 10) ||
                        (v1 == 0 && v2 == 15)
                        );
                }
                else if (flag == 2)
                {
                    this.Assert(
                        (v1 == 5 && v2 == 5) ||
                        (v1 == 0 && v2 == 15)
                        );
                }
                else if (flag == 3)
                {
                    this.Assert(
                        (v1 == 5 && v2 == 5) ||
                        (v1 == 0 && v2 == 10)
                        );
                }
            }


        }

        class N : Machine
        {
            ISharedCounter counter;

            [Start]
            [OnEventDoAction(typeof(Done), nameof(Check))]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                counter = (this.ReceivedEvent as E).counter;

                counter.Add(5);
            }

            void Check()
            {
                var v = counter.GetValue();
                this.Assert(v == 0);
            }
        }

        [Fact]
        public void TestCounterSuccess()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M), new Conf(0));
            });

            base.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestCounterFail1()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M), new Conf(1));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }

        [Fact]
        public void TestCounterFail2()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M), new Conf(2));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }


        [Fact]
        public void TestCounterFail3()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M), new Conf(3));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }

    }
}
