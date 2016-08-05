//-----------------------------------------------------------------------
// <copyright file="NoMemoryLeakAfterHaltTest.cs">
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
    public class NoMemoryLeakAfterHaltTest
    {
        internal class E : Event
        {
            public MachineId Id;

            public E(MachineId id)
                : base()
            {
                this.Id = id;
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
                while (counter < 100)
                {
                    var n = CreateMachine(typeof(N));
                    this.Send(n, new E(this.Id));
                    this.Receive(typeof(E));
                    counter++;
                    System.Console.WriteLine(counter);
                }
            }
        }

        class N : Machine
        {
            int[] large_array;

            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            [OnEntry(nameof(Configure))]
            class Init : MachineState { }

            void Configure()
            {
                large_array = new int[10000000];
                large_array[large_array.Length - 1] = 1;
            }

            void Act()
            {
                var sender = (this.ReceivedEvent as E).Id;
                this.Send(sender, new E(this.Id));
                Raise(new Halt());
            }
        }

        public static class Program
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(M), "TheUltimateServerMachine");
            }
        }

        [TestMethod]
        public void TestNoMemoryLeakAfterHalt()
        {
            var configuration = Configuration.Create();
            configuration.ThrowInternalExceptions = true;

            var runtime = PSharpRuntime.Create(configuration);
            Program.Execute(runtime);
            runtime.Wait();
        }
    }
}
