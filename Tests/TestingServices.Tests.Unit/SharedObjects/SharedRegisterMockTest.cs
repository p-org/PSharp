//-----------------------------------------------------------------------
// <copyright file="SharedRegisterMockTest.cs">
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
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class SharedRegisterMockTest : BaseTest
    {
        class E : Event
        {
            public ISharedRegister<int> counter;

            public E(ISharedRegister<int> counter)
            {
                this.counter = counter;
            }
        }

        class Eflag : Event
        {
            public bool flag;

            public Eflag(bool flag)
            {
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
                var flag = (this.ReceivedEvent as Eflag).flag;

                var counter = SharedObjects.CreateSharedRegister<int>(this.Id.Runtime, 0);
                counter.SetValue(5);

                this.CreateMachine(typeof(N), new E(counter));

                counter.Update(x =>
                {
                    if (x == 5)
                    {
                        return 6;
                    }
                    return x;
                });

                var v = counter.GetValue();

                if (flag)
                {
                    this.Assert(v == 2 || v == 6); //succeeds
                }
                else
                {
                    this.Assert(v == 6); // fails
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
                var counter = (this.ReceivedEvent as E).counter;
                counter.SetValue(2);
            }
        }

        [Fact]
        public void TestRegisterSuccess()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M), new Eflag(true));
            });

            base.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestRegisterFail()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M), new Eflag(false));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }
    }
}
