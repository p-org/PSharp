﻿using System;

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
