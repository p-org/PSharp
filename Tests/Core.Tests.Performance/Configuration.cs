// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    public class Configuration : ManualConfig
    {
        public Configuration()
        {
            Add(MemoryDiagnoser.Default);
            //Add(StatisticColumn.OperationsPerSecond);
        }
    }
}
