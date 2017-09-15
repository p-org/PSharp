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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Optional parameters for a Send operation
    /// </summary>
    public class SendOptions
    {
        /// <summary>
        /// Operation group ID
        /// </summary>
        Guid? operationGroupId = null;

        /// <summary>
        /// Is this a MustHandle event?
        /// </summary>
        bool mustHandle = false;

        SendOptions(Guid? operationGroupId, bool mustHandle)
        {
            this.operationGroupId = operationGroupId;
            this.mustHandle = mustHandle;
        }

        /// <summary>
        /// Adds the MustHandle option
        /// </summary>
        public static SendOptions MustHandle()
        {
            return new SendOptions(null, true);
        }

        /// <summary>
        /// Adds an operations group ID 
        /// </summary>
        /// <param name="operationGroupId">Operation group id</param>
        public static SendOptions OperationsGroupId(Guid operationGroupId)
        {
            return new SendOptions(operationGroupId, false);
        }

        /// <summary>
        /// Adds the MustHandle option
        /// </summary>
        public SendOptions WithMustHandle()
        {
            return new SendOptions(operationGroupId, true);
        }

        /// <summary>
        /// Adds an operations group ID
        /// </summary>
        /// <param name="operationGroupId">Optional operation group id</param>
        public SendOptions WithOperationsGroupId(Guid operationGroupId)
        {
            return new SendOptions(operationGroupId, mustHandle);
        }
    }
}