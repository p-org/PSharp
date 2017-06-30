//-----------------------------------------------------------------------
// <copyright file="OperationType.cs">
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

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// An operation used during scheduling.
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// starts executing.
        /// </summary>
        Start = 0,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// creates another <see cref="ISchedulable"/>.
        /// </summary>
        Create,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// sends an event to a target <see cref="ISchedulable"/>.
        /// </summary>
        Send,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// receives an event.
        /// </summary>
        Receive,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// stops executing.
        /// </summary>
        Stop,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// yields. This signals to the scheduler that the current
        /// <see cref="ISchedulable"/> is not making progress.
        /// An unfair scheduler could disable such <see cref="ISchedulable"/>s
        /// until quiescence, then re-enable them.
        /// Not currently supported in P#.
        /// </summary>
        Yield,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// wants to wait for quiescence.
        /// A scheduler could disable the <see cref="ISchedulable"/>
        /// until quiescence and then re-enable the <see cref="ISchedulable"/>.
        /// Not currently supported in P#.
        /// </summary>
        WaitForQuiescence,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// wants to wait for another <see cref="ISchedulable"/>
        /// to Stop.
        /// Not currently supported in P#.
        /// </summary>
        Join
    }
}
