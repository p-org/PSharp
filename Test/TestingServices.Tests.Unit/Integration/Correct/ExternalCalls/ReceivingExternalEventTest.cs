//-----------------------------------------------------------------------
// <copyright file="ReceivingExternalEventTest.cs">
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

using Microsoft.PSharp.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    [TestClass]
    public class ReceivingExternalEventTest
    {
        class E1 : Event
        {
            public int Value;

            public E1(int v)
                : base()
            {
                this.Value = v;
            }
        }

        class Engine
        {
            public static void Send(PSharpRuntime runtime, MachineId target)
            {
                runtime.SendEvent(target, new E1(2));
            }
        }

        class Real1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                Engine.Send(this.Runtime, this.Id);
            }

            void HandleEvent()
            {
                this.Assert((this.ReceivedEvent as E1).Value == 2);
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Real1));
            }
        }
        
        [TestMethod]
        public void TestReceivingExternalEvents()
        {
            var configuration = Configuration.Create();
            configuration.ThrowInternalExceptions = true;
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(0, engine.NumOfFoundBugs);
        }
    }
}
