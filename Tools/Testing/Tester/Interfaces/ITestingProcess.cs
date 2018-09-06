// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

#if NET46 || NET45
using System.ServiceModel;
#endif
using Microsoft.PSharp.TestingServices.Coverage;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Interface for a remote P# testing process.
    /// </summary>
#if NET46 || NET45
    [ServiceContract(Namespace = "Microsoft.PSharp")]
    [ServiceKnownType("GetKnownTypes", typeof(KnownTypesProvider))]
#endif
    internal interface ITestingProcess
    {
        /// <summary>
        /// Returns the test report.
        /// </summary>
        /// <returns>TestReport</returns>
#if NET46 || NET45
        [OperationContract]
#endif
        TestReport GetTestReport();

        /// <summary>
        /// Stops testing.
        /// </summary>
#if NET46 || NET45
        [OperationContract]
#endif
        void Stop();
    }
}
