//-----------------------------------------------------------------------
// <copyright file="ComponentBase.cs">
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
    /// Base class for <see cref="IChessCopComponent"/>.
    /// </summary>
    internal class CopComponentBase : ComponentBase, ICopComponent
    {
        ICopComponentServices _services;
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public new ICopComponentServices Services
        {
            get
            {
                if (this._services == null)
                    this._services = new CopComponentServices(this);
                return this._services;
            }
        }
    }
}
