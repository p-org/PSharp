//-----------------------------------------------------------------------
// <copyright file="EventOriginInfo.cs">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Defines an event origin info. Used
    /// during visualization.
    /// </summary>
    public class EventOriginInfo 
    {
        /// <summary>
        /// Sender machine.
        /// </summary>
        public string Machine { get; private set; }

        /// <summary>
        /// Sender machine state.
        /// </summary>
        public string State { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machine">Machine name</param>
        /// <param name="state">State name</param>
        public EventOriginInfo(string machine, string state)
        {
            this.Machine = machine;
            this.State = state;
        }
    }
}
