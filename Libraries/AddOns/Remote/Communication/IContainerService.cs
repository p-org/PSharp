//-----------------------------------------------------------------------
// <copyright file="IContainerService.cs">
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

using System.ServiceModel;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// Interface for sending notifications to a container.
    /// </summary>
    [ServiceContract(Namespace = "Microsoft.PSharp")]
    internal interface IContainerService
    {
        /// <summary>
        /// Notifies the container to start the P# runtime.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyStartPSharpRuntime();

        /// <summary>
        /// Notifies the container to terminate.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyTerminate();
    }
}
