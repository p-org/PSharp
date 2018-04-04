//-----------------------------------------------------------------------
// <copyright file="GotoStateTopLevelActionSuccessTest.cs">
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

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class GotoStateTopLevelActionSuccessTest : BaseTest
    {
        public enum ErrorType
        {
            CALL_GOTO,
            CALL_PUSH,
            CALL_RAISE,
            CALL_SEND,
            ON_EXIT
        };

        class Configure : Event
        {
            public ErrorType ErrorTypeVal;

            public Configure(ErrorType errorTypeVal)
            {
                this.ErrorTypeVal = errorTypeVal;
            }
        }

        class E : Event { }

        class Program : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [IgnoreEvents(typeof(E))]
            class Init : MachineState { }

            void EntryInit()
            {
                var errorType = (this.ReceivedEvent as Configure).ErrorTypeVal;
                this.Foo();
                this.ClearPreviousRaisedEvent();
                switch (errorType)
                {
                    case ErrorType.CALL_GOTO:
                        this.Goto<Done>();
                        break;
                    case ErrorType.CALL_PUSH:
                        this.Push<Done>();
                        break;
                    case ErrorType.CALL_RAISE:
                        this.Raise(new E());
                        break;
                    case ErrorType.CALL_SEND:
                        this.Send(Id, new E());
                        break;
                    case ErrorType.ON_EXIT:
                        break;
                }
            }

            void Foo()
            {
                this.Goto<Done>();
            }

            class Done : MachineState { }
        }

        [Fact]
        public void TestGotoStateTopLevelActionSuccess1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program), new Configure(ErrorType.CALL_GOTO));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestGotoStateTopLevelActionSuccess2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program), new Configure(ErrorType.CALL_RAISE));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestGotoStateTopLevelActionSuccess3()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program), new Configure(ErrorType.CALL_SEND));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestGotoStateTopLevelActionSuccess5()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program), new Configure(ErrorType.CALL_PUSH));
            });

            base.AssertSucceeded(test);
        }
    }
}