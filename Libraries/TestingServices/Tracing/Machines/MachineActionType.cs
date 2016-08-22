//-----------------------------------------------------------------------
// <copyright file="MachineActionType.cs">
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

using System.Runtime.Serialization;

namespace Microsoft.PSharp.TestingServices.Tracing.Machines
{
    /// <summary>
    /// The machine action type.
    /// </summary>
    [DataContract]
    public enum MachineActionType
    {
        /// <summary>
        /// An event sending action.
        /// </summary>
        [EnumMember(Value = "SendAction")]
        SendAction = 0,

        /// <summary>
        /// An invocation action.
        /// </summary>
        [EnumMember(Value = "InvocationAction")]
        InvocationAction,

        /// <summary>
        /// Task machine creation
        /// </summary>
        [EnumMember(Value = "TaskMachineCreation")]
        TaskMachineCreation
    }
}
