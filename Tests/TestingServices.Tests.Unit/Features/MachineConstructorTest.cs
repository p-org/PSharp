//-----------------------------------------------------------------------
// <copyright file="MachineConstructorTest.cs">
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
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class MachineConstructorTest : BaseTest
    {
        class E : Event
        {
            public MachineId Id;

            public E() { }

            public E(MachineId id)
            {
                Id = id;
            }
        }

        class M1 : Machine
        {
            protected int x = 5;

            [MachineConstructor]
            void Cons(Event e)
            {
                x = x * 2;
            }

        }
        class M2 : M1
        {
            [MachineConstructor]
            void Cons(Event e)
            {
                x = x + 2;
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Assert(x == 12);
            }
        }

        [Fact]
        public void TestConstructorCalled()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2));
            });

            AssertSucceeded(test);
        }


    }
}
