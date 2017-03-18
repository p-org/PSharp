//-----------------------------------------------------------------------
// <copyright file="ExceptionPropagationTest.cs">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    [TestClass]
    public class ExceptionPropagationTest
    {
        internal class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.TCS = tcs;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).TCS;
                try
                {
                    this.Assert(false);
                }
                finally
                {
                    tcs.SetResult(true);
                }
            }
        }

        class N: Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).TCS;
                try
                {
                    throw new InvalidOperationException();
                }
                finally
                {
                    tcs.SetResult(true);
                }
            }
        }

        public static class Program
        {
            public static void ExecuteM(PSharpRuntime runtime)
            {
                var tcs = new TaskCompletionSource<bool>();
                runtime.CreateMachine(typeof(M), new Configure(tcs));
                tcs.Task.Wait();
            }

            public static void ExecuteN(PSharpRuntime runtime)
            {
                var tcs = new TaskCompletionSource<bool>();
                runtime.CreateMachine(typeof(N), new Configure(tcs));
                tcs.Task.Wait();
            }
        }

        [TestMethod]
        public void TestAssertFailureNoEventHandler()
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            Program.ExecuteM(runtime);
        }

        [TestMethod]
        [ExpectedException(typeof(AssertionFailureException))]
        public void TestAssertFailureEventHandler()
        {
            var tcs = new TaskCompletionSource<bool>();

            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.OnFailure += delegate (Exception ex)
            {
                tcs.SetException(ex);
            };

            Program.ExecuteM(runtime);
            try
            {
                tcs.Task.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestUnhandledExceptionEventHandler()
        {
            var tcs = new TaskCompletionSource<bool>();

            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.OnFailure += delegate (Exception ex)
            {
                tcs.SetException(ex.InnerException);
            };

            Program.ExecuteN(runtime);
            try
            {
                tcs.Task.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
