//-----------------------------------------------------------------------
// <copyright file="GotoStateEvent.cs">
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
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The goto state event.
    /// </summary>
    [DataContract]
    internal sealed class GotoStateEvent : Event
    {
        /// <summary>
        /// Type of the state to transition to.
        /// </summary>
        public Type State;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="s">Type of the state</param>
        public GotoStateEvent(Type s)
            : base()
        {
            this.State = s;
        }
    }
}
