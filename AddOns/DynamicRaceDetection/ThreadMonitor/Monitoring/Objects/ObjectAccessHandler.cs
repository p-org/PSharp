//-----------------------------------------------------------------------
// <copyright file="ObjectAccessHandler.cs">
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

using Microsoft.ExtendedReflection.Monitoring;

namespace Microsoft.PSharp.Monitoring.CallsOnly
{
    /// <summary>
    /// Memory access delegate.
    /// </summary>
    /// <param name="address">base address of memory access</param>
    /// <param name="size">size of memory access operand</param>
    /// <param name="volatile">indicates if the access is volatile</param>
    /// <returns></returns>
    internal delegate Exception ObjectAccessHandler(GCAddress address, uint size, bool @volatile);

    /// <summary>
    /// Memory access delegate.
    /// </summary>
    /// <param name="interiorPointer">base address of memory access</param>
    /// <param name="size">size of memory access operand</param>
    /// <param name="volatile">indicates if the access is volatile</param>
    /// <returns></returns>
    internal delegate Exception RawAccessHandler(UIntPtr interiorPointer, uint size, bool @volatile);

    /// <summary>
    /// Delegate callback on New().
    /// </summary>
    /// <param name="allocatedObject"> Object that is currently allocated</param>
    /// <returns></returns>
    internal delegate Exception ObjectAllocationHandler(object allocatedObject);

}
