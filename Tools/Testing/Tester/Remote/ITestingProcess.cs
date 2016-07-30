//-----------------------------------------------------------------------
// <copyright file="ITestingProcess.cs">
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

using System.ServiceModel;

using Microsoft.PSharp.TestingServices.Coverage;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Interface for a remote P# testing process.
    /// </summary>
    [ServiceContract(Namespace = "Microsoft.PSharp")]
    [ServiceKnownType("GetKnownTypes", typeof(KnownTypesProvider))]
    internal interface ITestingProcess
    {
        /// <summary>
        /// Returns the test report.
        /// </summary>
        /// <returns>TestReport</returns>
        [OperationContract]
        TestReport GetTestReport();
    }
}
