//-----------------------------------------------------------------------
// <copyright file="ObjectAccessThreadMonitor.cs">
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
using System.Diagnostics;

using Microsoft.PSharp.Monitoring.ComponentModel;

namespace Microsoft.PSharp.Monitoring.CallsOnly
{
    /// <summary>
    /// Memory access monitor. 
    /// to enabled.
    /// </summary>
    /// <remarks>
    /// Notifes listener of read/write events
    /// </remarks>
    internal sealed class ObjectAccessThreadMonitor : ThreadMonitorBase
    {
        /// <summary>
        /// Raised on memory read access. Returns exception to throw if any.
        /// </summary>
        public static event RawAccessHandler ReadRawAccess;

        /// <summary>
        /// Raised on memory write access. Returns exception to throw if any.
        /// </summary>
        public static event RawAccessHandler WriteRawAccess;

        /// <summary>
        /// Raised after object allocation. Returns exception to throw if any.
        /// </summary>
        public static event ObjectAllocationHandler ObjectAllocationHandlerEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectAccessThreadMonitor"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        internal ObjectAccessThreadMonitor(ICopComponent host)
            : base(host)
        { }

        /// <summary>
        /// Notifies a write access
        /// </summary>
        /// <param name="interiorPointer">The interior pointer.</param>
        /// <param name="size">The size.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        /// <returns></returns>
        [DebuggerNonUserCodeAttribute]
        public override Exception Store(UIntPtr interiorPointer, uint size, bool @volatile)
        {
            RawAccessHandler rh = WriteRawAccess;

            if (rh != null)
            {
                return rh(interiorPointer, size, @volatile);
            }

            return null;
        }

        /// <summary>
        /// Notifies a read access
        /// </summary>
        /// <param name="interiorPointer">The interior pointer.</param>
        /// <param name="size">The size.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        /// <returns></returns>
        [DebuggerNonUserCodeAttribute]
        public override Exception Load(UIntPtr interiorPointer, uint size, bool @volatile)
        {
            RawAccessHandler rh = ReadRawAccess;

            if (rh != null)
            {
                return rh(interiorPointer, size, @volatile);
            }

            return null;
        }

        [DebuggerNonUserCodeAttribute]
        public override Exception ObjectAllocationAccess(object newObject)
        {
            ObjectAllocationHandler rh = ObjectAllocationHandlerEvent;

            if (rh != null)
            {
                return rh(newObject);
            }

            return null;
        }
    }
}
