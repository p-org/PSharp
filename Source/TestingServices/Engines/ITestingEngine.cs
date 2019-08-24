// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Interface of a P# testing engine.
    /// </summary>
    public interface ITestingEngine
    {
        /// <summary>
        /// Data structure containing information gathered during testing.
        /// </summary>
        TestReport TestReport { get; }

        /// <summary>
        /// Interface for registering runtime operations.
        /// </summary>
        IRegisterRuntimeOperation Reporter { get; }

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        ITestingEngine Run();

        /// <summary>
        /// Stops the P# testing engine.
        /// </summary>
        void Stop();

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        string GetReport();

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        void TryEmitTraces(string directory, string file);

        /// <summary>
        /// Registers a callback to invoke at the end of each iteration. The callback
        /// takes as a parameter an integer representing the current iteration.
        /// </summary>
        void RegisterPerIterationCallBack(Action<int> callback);
    }
}
