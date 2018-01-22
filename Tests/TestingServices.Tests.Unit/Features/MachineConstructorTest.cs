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
using System.Collections.Generic;
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
        public void TestConstructorCalled1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2));
            });

            AssertSucceeded(test);
        }


        class M3 : Machine
        {
            protected int x = 5;

            [MachineConstructor]
            async Task Cons(Event e)
            {
                await Task.Yield();
                x = x * 2;
            }

        }
        class M4 : M3
        {
            [MachineConstructor]
            async Task Cons(Event e)
            {
                await Task.Yield();
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
        public void TestConstructorCalled2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M4));
            });

            AssertSucceeded(test);
        }

        class IncorrectDecl1 : Machine
        {
            [Start]
            class Init : MachineState { }

            [MachineConstructor]
            bool IncorrectReturn(Event e)
            {
                return false;
            }
        }

        [Fact]
        public void TestIncorrectDecl1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(IncorrectDecl1));
            });

            var exptectedOutputs = new HashSet<string> { "Method IncorrectReturn of class IncorrectDecl1, marked with attribute MachineConstructor must have return type either void or Task" ,
                "Machine construction failed for IncorrectDecl1" };
            var config = Configuration.Create();

            AssertFailed(config, test, 1, new Func<HashSet<string>, bool>(bugReports => bugReports.IsSubsetOf(exptectedOutputs)), true);
        }
    }
}
