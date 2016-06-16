//-----------------------------------------------------------------------
// <copyright file="IManagerService.cs">
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
    /// Interface for sending notifications to the remote manager.
    /// </summary>
    [ServiceContract(Namespace = "Microsoft.PSharp")]
    internal interface IManagerService
    {
        /// <summary>
        /// Notifies the manager that the container was initialized.
        /// </summary>
        /// <param name="id">Container id</param>
        [OperationContract(IsOneWay = true)]
        void NotifyInitialized(int id);
    }
}
