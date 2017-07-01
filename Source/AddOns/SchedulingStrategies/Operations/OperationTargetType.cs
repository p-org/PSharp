//-----------------------------------------------------------------------
// <copyright file="OperationTargetType.cs">
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

namespace Microsoft.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// The target of an operation used during scheduling.
    /// </summary>
    public enum OperationTargetType
    {
        /// <summary>
        /// The target of the operation is an <see cref="ISchedulable"/>.
        /// For example, 'Create', 'Start' and 'Stop' are operations that
        /// act upon an <see cref="ISchedulable"/>.
        /// </summary>
        Schedulable = 0,
        /// <summary>
        /// The target of the operation is the inbox of an <see cref="ISchedulable"/>.
        /// For example, 'Send' and 'Receive' are operations that act upon the
        /// inbox of an <see cref="ISchedulable"/>.
        /// </summary>
        Inbox
    }
}