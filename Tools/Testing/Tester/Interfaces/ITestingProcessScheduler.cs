//-----------------------------------------------------------------------
// <copyright file="ITestingScheduler.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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

#if NET46 || NET45
using System.ServiceModel;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Interface for a remote P# testing process scheduler.
    /// </summary>
    [ServiceContract(Namespace = "Microsoft.PSharp")]
    [ServiceKnownType("GetKnownTypes", typeof(KnownTypesProvider))]
    internal interface ITestingProcessScheduler
    {
        /// <summary>
        /// Notifies the testing process scheduler
        /// that a bug was found.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>Boolean value</returns>
        [OperationContract]
        void NotifyBugFound(uint processId);

        /// <summary>
        /// Sets the test report from the specified process.
        /// </summary>
        /// <param name="testReport">TestReport</param>
        /// <param name="processId">Unique process id</param>
        [OperationContract]
        void SetTestReport(TestReport testReport, uint processId);
    }
}
#endif
