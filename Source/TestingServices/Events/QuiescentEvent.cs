//-----------------------------------------------------------------------
// <copyright file="QuiescentEvent.cs">
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

using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Signals that a machine has reached Quiescence
    /// </summary>
    [DataContract]
    internal sealed class QuiescentEvent : Event
    {
        /// <summary>
        /// MachineId
        /// </summary>
        public MachineId mid;

        /// <summary>
        /// Constructor.
        /// </summary>
        public QuiescentEvent(MachineId mid)
            : base()
        {
            this.mid = mid;
        }
    }
}
