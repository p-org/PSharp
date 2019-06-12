// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

#if NET46
using System.ServiceModel;

using Microsoft.PSharp.TestingServices.Coverage;
#endif

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Interface for a remote P# testing process.
    /// </summary>
#if NET46
    [ServiceContract(Namespace = "Microsoft.PSharp")]
    [ServiceKnownType(typeof(TestReport))]
    [ServiceKnownType(typeof(CoverageInfo))]
    [ServiceKnownType(typeof(Transition))]
#endif
    internal interface ITestingProcess
    {
        /// <summary>
        /// Returns the test report.
        /// </summary>
#if NET46
        [OperationContract]
#endif
        TestReport GetTestReport();

        /// <summary>
        /// Stops testing.
        /// </summary>
#if NET46
        [OperationContract]
#endif
        void Stop();
    }
}
