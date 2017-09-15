//-----------------------------------------------------------------------
// <copyright file="SendOptions.cs">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Optional parameters for a send operation.
    /// </summary>
    public class SendOptions
    {
        /// <summary>
        /// Operation group id.
        /// </summary>
        public Guid? OperationGroupId;

        /// <summary>
        /// Is this a MustHandle event?
        /// </summary>
        public bool MustHandle;

        /// <summary>
        /// Default options.
        /// </summary>
        public SendOptions()
        {
            OperationGroupId = null;
            MustHandle = false;
        }

        /// <summary>
        /// A string that represents the current options.
        /// </summary>
        public override string ToString()
        {
            return $"SendOptions[Guid='{OperationGroupId}', MustHandle='{MustHandle}']";
        }

        /// <summary>
        /// Implicit conversion from a Guid.
        /// </summary>
        public static implicit operator SendOptions(Guid operationGroupId)
        {
            return new SendOptions { OperationGroupId = operationGroupId };
        }
    }
}
