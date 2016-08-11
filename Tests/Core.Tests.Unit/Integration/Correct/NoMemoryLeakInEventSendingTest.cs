//-----------------------------------------------------------------------
// <copyright file="NoMemoryLeakInEventSendingTest.cs">
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

using Microsoft.PSharp.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    [TestClass]
    public class NoMemoryLeakInEventSendingTest
    {
        internal class E : Event
        {
            public MachineId Id;
            int[] LargeArray;

            public E(MachineId id)
                : base()
            {
                this.Id = id;
                this.LargeArray = new int[10000000];
            }
        }

        internal class Unit : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                int counter = 0;
                var n = CreateMachine(typeof(N));

                while (counter < 1000)
                {
                    this.Send(n, new E(this.Id));
                    this.Receive(typeof(E));
                    counter++;
                }
            }
        }

        class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            class Init : MachineState { }

            void Act()
            {
                var sender = (this.ReceivedEvent as E).Id;
                this.Send(sender, new E(this.Id));
            }
        }

        public static class Program
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(M));
            }
        }

        [TestMethod]
        public void TestNoMemoryLeakInEventSending()
        {
            var runtime = PSharpRuntime.Create();
            Program.Execute(runtime);
            runtime.Wait();
        }
    }
}
