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
            void Cons()
            {
                x = x * 2;
            }

        }
        class M2 : M1
        {
            [MachineConstructor]
            void Cons()
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
            async Task Cons()
            {
                await Task.Yield();
                x = x * 2;
            }

        }
        class M4 : M3
        {
            [MachineConstructor]
            async Task Cons()
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
            bool IncorrectReturn()
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

            // The replay engine gives a different error message
            var exptectedOutputs = new HashSet<string> { "Method IncorrectReturn of class IncorrectDecl1, marked with attribute MachineConstructor must have return type either void or Task" ,
                "Machine construction failed for IncorrectDecl1" };
            var config = Configuration.Create();

            AssertFailed(config, test, 1, new Func<HashSet<string>, bool>(bugReports => bugReports.IsSubsetOf(exptectedOutputs)), true);
        }

        class IncorrectDecl2 : Machine
        {
            [Start]
            class Init : MachineState { }

            [MachineConstructor]
            async Task IncorrectArg(E e)
            {
                await Task.Yield();
            }
        }

        [Fact]
        public void TestIncorrectDecl2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(IncorrectDecl2));
            });

            // The replay engine gives a different error message
            var exptectedOutputs = new HashSet<string> { "Method IncorrectArg of class IncorrectDecl2, marked with attribute MachineConstructor cannot accept parameters" ,
                "Machine construction failed for IncorrectDecl2" };
            var config = Configuration.Create();

            AssertFailed(config, test, 1, new Func<HashSet<string>, bool>(bugReports => bugReports.IsSubsetOf(exptectedOutputs)), true);
        }

        class IncorrectDecl3 : Machine
        {
            [Start]
            class Init : MachineState { }

            [MachineConstructor]
            async Task IncorrectArgs(Event e, E e2)
            {
                await Task.Yield();
            }
        }

        [Fact]
        public void TestIncorrectDecl3()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(IncorrectDecl3));
            });

            // The replay engine gives a different error message
            var exptectedOutputs = new HashSet<string> { "Method IncorrectArgs of class IncorrectDecl3, marked with attribute MachineConstructor cannot accept parameters" ,
                "Machine construction failed for IncorrectDecl3" };
            var config = Configuration.Create();

            AssertFailed(config, test, 1, new Func<HashSet<string>, bool>(bugReports => bugReports.IsSubsetOf(exptectedOutputs)), true);
        }

        class IncorrectDecl4 : Machine
        {
            [Start]
            class Init : MachineState { }

            [MachineConstructor]
            async Task Cons1(Event e)
            {
                await Task.Yield();
            }


            [MachineConstructor]
            async Task Cons2(Event e)
            {
                await Task.Yield();
            }
        }

        [Fact]
        public void TestIncorrectDecl4()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(IncorrectDecl4));
            });

            // The replay engine gives a different error message
            var exptectedOutputs = new HashSet<string> {
                "Only one instance member of class IncorrectDecl4 can be marked with attribute MachineConstructor." + Environment.NewLine +
                "Found Cons1" + Environment.NewLine +
                "Found Cons2",
                "Machine construction failed for IncorrectDecl4" };
            var config = Configuration.Create();

            AssertFailed(config, test, 1, new Func<HashSet<string>, bool>(bugReports => bugReports.IsSubsetOf(exptectedOutputs)), true);
        }
    }
}
