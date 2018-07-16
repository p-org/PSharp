//-----------------------------------------------------------------------
// <copyright file="EntryPointThrowExceptionTest.cs">
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
    public class EntryPointThrowExceptionTest : BaseTest
    {
        class M : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        [Fact]
        public void TestEntryPointThrowException()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                MachineId m = r.CreateMachine(typeof(M));
                throw new InvalidOperationException();
            });

            base.AssertFailedWithException(test, typeof(InvalidOperationException), true);
        }

        [Fact]
        public void TestEntryPointNoMachinesThrowException()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                throw new InvalidOperationException();
            });

            base.AssertFailedWithException(test, typeof(InvalidOperationException), true);
        }
    }
}
