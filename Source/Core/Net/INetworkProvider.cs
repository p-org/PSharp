//-----------------------------------------------------------------------
// <copyright file="INetworkProvider.cs">
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
using System.Threading.Tasks;

namespace Microsoft.PSharp.Net
{
    /// <summary>
    /// Interface of a P# network provider that is responsible for
    /// creating machines and sending events across a network.
    /// </summary>
    public interface INetworkProvider : IDisposable
    {
        /// <summary>
        /// The local endpoint.
        /// </summary>
        string LocalEndpoint { get; }

        /// <summary>
        /// Creates a new <see cref="MachineId"/> for the specified machine type.
        /// </summary>
        /// <param name="machineType">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        Task<MachineId> CreateMachineId(Type machineType, string friendlyName);

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>,
        /// at the endpoint specified by the <see cref="MachineId"/>. This method optionally passes
        /// an <see cref="Event"/> to the new machine, which can only be used to access its payload,
        /// and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="machineType">Type of the machine.</param>
        /// <param name="e">Event to send to the machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        Task CreateMachine(MachineId mid, Type machineType, Event e);

        /// <summary>
        /// Sends an event to a machine at the endpoint specified by
        /// the <see cref="MachineId"/>.
        /// </summary>
        /// <param name="target">Target machine.</param>
        /// <param name="e">Event to send to the machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        Task Send(MachineId target, Event e);

        /// <summary>
        /// Checks if the specified <see cref="MachineId"/> belongs to a local machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <returns>true if the machine id belongs to a local machine; else false.</returns>
        bool IsLocalMachine(MachineId mid);
    }
}
