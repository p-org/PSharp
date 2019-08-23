﻿#if NET46
using System.ServiceModel;
using Microsoft.PSharp.TestingServices.Coverage;
#endif

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Interface for a remote P# testing process scheduler.
    /// </summary>
#if NET46
    [ServiceContract(Namespace = "Microsoft.PSharp")]
    [ServiceKnownType(typeof(TestReport))]
    [ServiceKnownType(typeof(CoverageInfo))]
    [ServiceKnownType(typeof(Transition))]
#endif
    internal interface ITestingProcessScheduler
    {
        /// <summary>
        /// Notifies the testing process scheduler
        /// that a bug was found.
        /// </summary>
#if NET46
        [OperationContract]
#endif
        void NotifyBugFound(uint processId);

        /// <summary>
        /// Sets the test report from the specified process.
        /// </summary>
#if NET46
        [OperationContract]
#endif
        void SetTestReport(TestReport testReport, uint processId);
    }
}
