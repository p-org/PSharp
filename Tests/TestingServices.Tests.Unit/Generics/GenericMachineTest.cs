//-----------------------------------------------------------------------
// <copyright file="GenericMachineTest.cs">
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
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class GenericMachineTest : BaseTest
    {
        public GenericMachineTest(ITestOutputHelper output)
            : base(output)
        { }

        class M<T> : Machine
        {
            T Item;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Item = default(T);
                this.Goto<Active>();
            }

            [OnEntry(nameof(ActiveInit))]
            class Active : MachineState { }

            void ActiveInit()
            {
                this.Assert(this.Item is int);
            }
        }

        class N : M<int>
        {

        }

        [Fact]
        public void TestGenericMachine1()
        {
            var test = new Action<IPSharpRuntime>((r) => { r.CreateMachine(typeof(M<int>)); });
            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestGenericMachine2()
        {
            var test = new Action<IPSharpRuntime>((r) => { r.CreateMachine(typeof(N)); });
            base.AssertSucceeded(test);
        }
    }
}
