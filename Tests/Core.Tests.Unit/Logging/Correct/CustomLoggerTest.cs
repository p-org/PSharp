//-----------------------------------------------------------------------
// <copyright file="CustomLoggerTest.cs">
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

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    [TestClass]
    public class CustomLoggerTest
    {
        class CustomLogger : ILogger
        {
            private StringBuilder StringBuilder;

            public CustomLogger()
            {
                StringBuilder = new StringBuilder();
            }

            public void Write(string value)
            {
                StringBuilder.Append(value);
            }

            public void Write(string format, params object[] args)
            {
                StringBuilder.AppendFormat(format, args);
            }

            public void WriteLine(string value)
            {
                StringBuilder.AppendLine(value);
            }

            public void WriteLine(string format, params object[] args)
            {
                StringBuilder.AppendFormat(format, args);
                StringBuilder.AppendLine();
            }

            public override string ToString()
            {
                return StringBuilder.ToString();
            }

            public void Dispose()
            {
                StringBuilder.Clear();
                StringBuilder = null;
            }
        }

        internal class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.TCS = tcs;
            }
        }

        internal class E : Event
        {
            public MachineId Id;

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        internal class Unit : Event { }

        class M : Machine
        {
            TaskCompletionSource<bool> TCS;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                TCS = (this.ReceivedEvent as Configure).TCS;
                var n = CreateMachine(typeof(N));
                this.Send(n, new E(this.Id));
            }

            void Act()
            {
                TCS.SetResult(true);
            }
        }

        class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            class Init : MachineState { }

            void Act()
            {
                MachineId m = (this.ReceivedEvent as E).Id;
                this.Send(m, new E(this.Id));
            }
        }

        public static class Program
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                var tcs = new TaskCompletionSource<bool>();
                runtime.CreateMachine(typeof(M), new Configure(tcs));
                tcs.Task.Wait();
            }
        }

        [TestMethod]
        public void TestCustomLogger()
        {
            CustomLogger logger = new CustomLogger();

            Configuration config = Configuration.Create().WithVerbosityEnabled(2);
            PSharpRuntime runtime = PSharpRuntime.Create(config);
            runtime.SetLogger(logger);

            Program.Execute(runtime);

            string expected = @"<CreateLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M()' is created.
<StateLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M()' enters state 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M.Init'.
<ActionLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M()' invoked action 'InitOnEntry' in state 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M.Init'.
<CreateLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+N()' is created.
<StateLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+N()' enters state 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+N.Init'.
<SendLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M()' sent event 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+E' to 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+N()'.
<EnqueueLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+N()' enqueued event 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+E'.
<DequeueLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+N()' dequeued event 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+E'.
<ActionLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+N()' invoked action 'Act' in state 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+N.Init'.
<SendLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+N()' sent event 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+E' to 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M()'.
<EnqueueLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M()' enqueued event 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+E'.
<DequeueLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M()' dequeued event 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+E'.
<ActionLog> Machine 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M()' invoked action 'Act' in state 'Microsoft.PSharp.Core.Tests.Unit.CustomLoggerTest+M.Init'.
";

            string actual = Regex.Replace(logger.ToString(), "[0-9]", "");

            HashSet<string> expectedSet = new HashSet<string>(Regex.Split(expected, "\r\n|\r|\n"));
            HashSet<string> actualSet = new HashSet<string>(Regex.Split(actual, "\r\n|\r|\n"));

            Assert.IsTrue(expectedSet.SetEquals(actualSet));

            runtime.RemoveLogger();
        }

        [TestMethod]
        public void TestCustomLoggerNoVerbosity()
        {
            CustomLogger logger = new CustomLogger();

            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.SetLogger(logger);

            Program.Execute(runtime);

            Assert.AreEqual("", logger.ToString());

            runtime.RemoveLogger();
        }
    }
}
