//-----------------------------------------------------------------------
// <copyright file="CreateMachineOptions.cs">
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
    /// Optional parameters for a CreateMachine operation
    /// </summary>
    public class CreateMachineOptions
    {
        /// <summary>
        /// Operation group ID
        /// </summary>
        Guid? operationGroupId = null;

        /// <summary>
        /// Friendly name
        /// </summary>
        string friendlyName = null;

        /// <summary>
        /// Endpoint
        /// </summary>
        string endpoint = null;

        CreateMachineOptions(Guid? operationGroupId, string friendlyName, string endpoint)
        {
            this.operationGroupId = operationGroupId;
            this.friendlyName = friendlyName;
            this.endpoint = endpoint;
        }

        /// <summary>
        /// Adds a friendly name for the machine
        /// </summary>
        /// <param name="friendlyName">Friendly name</param>
        public static CreateMachineOptions FriendlyName(string friendlyName)
        {
            return new CreateMachineOptions(null, friendlyName, null);
        }

        /// <summary>
        /// Adds an operations group ID
        /// </summary>
        /// <param name="operationGroupId">Operation group id</param>
        public static CreateMachineOptions OperationsGroupId(Guid operationGroupId)
        {
            return new CreateMachineOptions(operationGroupId, null, null);
        }

        /// <summary>
        /// Adds an endpoint for a remote machine creation
        /// </summary>
        /// <param name="endpoint">Machine endpoint</param>
        public static CreateMachineOptions EndPoint(string endpoint)
        {
            return new CreateMachineOptions(null, null, endpoint);
        }

        /// <summary>
        /// Adds a friendly name for the machine
        /// </summary>
        /// <param name="friendlyName">Friendly name</param>
        public CreateMachineOptions WithFriendlyName(string friendlyName)
        {
            return new CreateMachineOptions(operationGroupId, friendlyName, endpoint);
        }

        /// <summary>
        /// Adds an operations group ID
        /// </summary>
        /// <param name="operationGroupId">Operation group id</param>
        public CreateMachineOptions WithOperationsGroupId(Guid operationGroupId)
        {
            return new CreateMachineOptions(operationGroupId, friendlyName, endpoint);
        }

        /// <summary>
        /// Adds an endpoint for a remote machine creation
        /// </summary>
        /// <param name="endpoint">Machine endpoint</param>
        public CreateMachineOptions WithEndPoint(string endpoint)
        {
            return new CreateMachineOptions(operationGroupId, friendlyName, endpoint);
        }
    }
}