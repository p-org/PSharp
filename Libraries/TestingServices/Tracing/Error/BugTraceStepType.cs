//-----------------------------------------------------------------------
// <copyright file="BugTraceStepType.cs">
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

namespace Microsoft.PSharp.TestingServices.Tracing.Error
{
    /// <summary>
    /// The bug trace step type.
    /// </summary>
    [DataContract]
    internal enum BugTraceStepType
    {
        [EnumMember(Value = "CreateMachine")]
        CreateMachine = 0,
        [EnumMember(Value = "CreateMonitor")]
        CreateMonitor,
        [EnumMember(Value = "SendEvent")]
        SendEvent,
        [EnumMember(Value = "DequeueEvent")]
        DequeueEvent,
        [EnumMember(Value = "RaiseEvent")]
        RaiseEvent,
        [EnumMember(Value = "InvokeAction")]
        InvokeAction,
        [EnumMember(Value = "RandomChoice")]
        RandomChoice
    }
}
