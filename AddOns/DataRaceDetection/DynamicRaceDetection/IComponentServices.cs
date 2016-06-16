//-----------------------------------------------------------------------
// <copyright file="IComponentServices.cs">
//      Copyright (c) 2016 Microsoft Corporation. All rights reserved.
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

using Microsoft.ExtendedReflection.ComponentModel;

namespace Microsoft.PSharp.DynamicRaceDetection.ComponentModel
{
    /// <summary>
    /// Services available in ChessCop components
    /// </summary>
    internal interface ICopComponentServices : IComponentServices
    {

        /// <summary>
        /// Gets the monitor manager.
        /// </summary>
        /// <value>The monitor manager.</value>
        IMonitorManager MonitorManager { get; }
    }
}
