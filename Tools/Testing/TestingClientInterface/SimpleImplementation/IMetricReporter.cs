// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingClientInterface.SimpleImplementation
{
    public interface IMetricReporter
    {
        void RecordIteration(ISchedulingStrategy strategy, bool bugFound);

        string GetReport();
    }
}
