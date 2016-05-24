//-----------------------------------------------------------------------
// <copyright file="DefaultNetworkProvider.cs">
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

namespace Microsoft.PSharp.Net
{
    /// <summary>
    /// Class implementing the default P# network provider.
    /// </summary>
    internal class DefaultNetworkProvider : INetworkProvider
    {
        #region fields

        /// <summary>
        /// Instance of the P# runtime.
        /// </summary>
        private PSharpRuntime Runtime;

        /// <summary>
        /// The local endpoint.
        /// </summary>
        private string LocalEndPoint;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        public DefaultNetworkProvider(PSharpRuntime runtime)
        {
            this.Runtime = runtime;
            this.LocalEndPoint = "";
        }

        #endregion

        #region methods

        /// <summary>
        /// Creates a new remote machine of the given
        /// type and with the given event.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns> 
        MachineId INetworkProvider.RemoteCreateMachine(Type type, string endpoint, Event e)
        {
            return this.Runtime.CreateMachine(type);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        void INetworkProvider.RemoteSend(MachineId target, Event e)
        {
            this.Runtime.SendEvent(target, e);
        }

        /// <summary>
        /// Returns the local endpoint.
        /// </summary>
        /// <returns>Endpoint</returns>
        string INetworkProvider.GetLocalEndPoint()
        {
            return this.LocalEndPoint;
        }

        #endregion
    }
}
