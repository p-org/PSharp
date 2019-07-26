// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
#pragma warning disable CA1040 // Avoid empty interfaces
    /// <summary>
    /// A representation of a step in the program.
    /// Used to identify corresponding steps across runs.
    /// Two corresponding steps must have equal StepSignature ( according to the Equals method )
    /// </summary>
    public interface IProgramStepSignature
#pragma warning restore CA1040 // Avoid empty interfaces
    {
        // The Equals method is to compare steps across runs
    }
}
