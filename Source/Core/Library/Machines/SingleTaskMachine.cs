//-----------------------------------------------------------------------
// <copyright file="SingleTaskMachine.cs">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Machine that hosts a single task
    /// </summary>
    internal class SingleTaskMachine : Machine
    {
        [Start]
        [OnEntry(nameof(Run))]
        class InitState : MachineState {  }

        /// <summary>
        /// Executes the payload
        /// </summary>
        async Task Run()
        {
            var function = (this.ReceivedEvent as SingleTaskMachineEvent).function;
            await function(this);
            this.Raise(new Halt());
        }
    }
}
