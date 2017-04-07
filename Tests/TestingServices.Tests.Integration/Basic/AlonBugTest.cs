//-----------------------------------------------------------------------
// <copyright file="AlonBugTest.cs">
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

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class AlonBugTest : BaseTest
    {
        class E : Event { }

        class Program : Machine
        {
            int i;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventPushState(typeof(E), typeof(Call))] // Exit does not execute.
            class Init : MachineState { }

            void EntryInit()
            {
                i = 0;
                this.Raise(new E());
            }

            void ExitInit()
            {
                // This assert is unreachable.
                this.Assert(false, "Bug found.");
            }

            [OnEntry(nameof(EntryCall))]
            class Call : MachineState { }

            void EntryCall()
            {
                if (i == 3)
                {
                    this.Pop();
                    return; // important if not compiling
                }
                else
                {
                    i = i + 1;
                }

                this.Raise(new E()); // Call is popped.
            }
        }

        [Fact]
        public void TestAlonBug()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program));
            });

            base.AssertSucceeded(test);
        }
    }
}
