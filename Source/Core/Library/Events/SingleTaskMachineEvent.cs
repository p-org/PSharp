﻿//-----------------------------------------------------------------------
// <copyright file="SingleTaskMachineEvent.cs">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Event carrying payload for <see cref="SingleTaskMachine"/>
    /// </summary>
    internal class SingleTaskMachineEvent : Event
    {
        /// <summary>
        /// Payload
        /// </summary>
        public Func<Machine, Task> function;

        /// <summary>
        /// Constructor
        /// </summary>
        public SingleTaskMachineEvent(Func<Machine, Task> function)
        {
            this.function = function;
        }
    }
}
