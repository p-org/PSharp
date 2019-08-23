// ------------------------------------------------------------------------------------------------

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
        [EnumMember(Value = "GotoState")]
        GotoState,
        [EnumMember(Value = "InvokeAction")]
        InvokeAction,
        [EnumMember(Value = "WaitToReceive")]
        WaitToReceive,
        [EnumMember(Value = "ReceiveEvent")]
        ReceiveEvent,
        [EnumMember(Value = "RandomChoice")]
        RandomChoice,
        [EnumMember(Value = "Halt")]
        Halt
    }
}
