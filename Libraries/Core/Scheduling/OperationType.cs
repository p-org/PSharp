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

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Operation type used during scheduling.
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// The 'start' operation type.
        /// </summary>
        Start = 0,
        /// <summary>
        /// The 'create' operation type.
        /// </summary>
        Create,
        /// <summary>
        /// The 'send' operation type.
        /// </summary>
        Send,
        /// <summary>
        /// The 'receive' operation type.
        /// </summary>
        Receive,
        /// <summary>
        /// The 'halt' operation type.
        /// </summary>
        Halt
    }
}
