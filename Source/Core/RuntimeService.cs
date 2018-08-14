//-----------------------------------------------------------------------
// <copyright file="RuntimeService.cs">
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

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# runtime service.
    /// </summary>
    public static class RuntimeService
    {
        /// <summary>
        /// Creates a new state-machine runtime.
        /// </summary>
        /// <returns>The P# runtime.</returns>
        public static IPSharpRuntime Create()
        {
            return new ProductionRuntime(Configuration.Create());
        }

        /// <summary>
        /// Creates a new state-machine runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        /// <returns>The P# runtime.</returns>
        public static IPSharpRuntime Create(Configuration configuration)
        {
            return new ProductionRuntime(configuration);
        }

        /// <summary>
        /// Returns the runtime associated with the specified <see cref="MachineId"/>.
        /// </summary>
        /// <param name="mid">The id of the machine.</param>
        /// <returns>The P# runtime.</returns>
        public static IPSharpRuntime GetRuntime(MachineId mid)
        {
            return mid.RuntimeManager.Runtime;
        }
    }
}
