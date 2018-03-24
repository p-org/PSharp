//-----------------------------------------------------------------------
// <copyright file="ResumeEvent.cs">
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
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The Resume event.
    /// </summary>
    [DataContract]
    public sealed class ResumeEvent : Event
    {
        /// <summary>
        /// State stack to resume from (first element is the bottom of the stack)
        /// </summary>
        public readonly List<string> StateStack;

        /// <summary>
        /// Starting event
        /// </summary>
        public readonly Event StartingEvent;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stateStack">The stack with which the machine will start execution</param>
        /// <param name="startingEvent">Optional starting event (optional)</param>
        public ResumeEvent(List<string> stateStack, Event startingEvent = null)
            : base()
        {
            this.StateStack = stateStack;
            this.StartingEvent = startingEvent;
        }
    }
}
