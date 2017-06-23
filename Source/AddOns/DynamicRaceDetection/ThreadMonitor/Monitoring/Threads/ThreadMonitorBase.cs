//-----------------------------------------------------------------------
// <copyright file="ThreadMonitorBase.cs">
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

using System;

using Microsoft.ExtendedReflection.ComponentModel;

using Microsoft.PSharp.Monitoring.ComponentModel;

namespace Microsoft.PSharp.Monitoring.CallsOnly
{
    /// <summary>
    /// Abstract base class to help implement
    /// <see cref="IThreadMonitor"/>
    /// </summary>
    internal abstract class ThreadMonitorBase : ComponentElementBase, IThreadMonitor
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadMonitorBase"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        protected ThreadMonitorBase(ICopComponent host)
            : base(host)
        {

        }

        #endregion

        #region methods

        /// <summary>
        /// This method is called when a (non-local) memory location is loaded from.
        /// </summary>
        /// <param name="location">An identifier of the memory address.</param>
        /// <param name="size">The size of the data loaded.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        [global::System.Diagnostics.DebuggerNonUserCode]
        public virtual Exception Load(UIntPtr location, uint size, bool @volatile) { return null; }

        /// <summary>
        /// This method is called when a (non-local) memory location is stored to.
        /// </summary>
        /// <param name="location">An identifier of the memory address.</param>
        /// <param name="size">The size of the data stored.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        [global::System.Diagnostics.DebuggerNonUserCode]
        public virtual Exception Store(UIntPtr location, uint size, bool @volatile) { return null; }

        /// <summary>
        /// This method is called after an object allocation.
        /// </summary>
        /// <param name="newObject">allocated object</param>
        /// <returns></returns>
        [global::System.Diagnostics.DebuggerNonUserCode]
        public virtual Exception ObjectAllocationAccess(object newObject) { return null; }

        /// <summary>
        /// Null out references to the testee so that the dispose tracker
        /// can do its work.
        /// This is a good place to log details.
        /// </summary>
        /// <remarks>
        /// Only RunCompleted is called afterwards.
        /// </remarks>
        public virtual void DisposeTesteeReferences() { }

        /// <summary>
        /// Program under test terminated.
        /// This is a good place to log summaries.
        /// </summary>
        public virtual void RunCompleted() { }

        /// <summary>
        /// Thread destroyed.
        /// </summary>
        public virtual void Destroy() { }

        #endregion
    }
}
