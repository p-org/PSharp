//-----------------------------------------------------------------------
// <copyright file="TimerExceptionTests.cs">
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class TimerExceptionTests
    {
		class NonMachineSubClass { }

		[Fact]
		public void TestExceptionOnInvalidTimerType()
		{
			PSharpRuntime runtime = PSharpRuntime.Create();

			Exception ex = Assert.Throws<AssertionFailureException>(() => runtime.SetTimerMachineType(typeof(NonMachineSubClass)));
		}

    }
}
