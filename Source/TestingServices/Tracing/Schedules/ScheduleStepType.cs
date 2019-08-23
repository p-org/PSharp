// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.TestingServices.Tracing.Schedule
{
    /// <summary>
    /// The schedule step type.
    /// </summary>
    internal enum ScheduleStepType
    {
        SchedulingChoice = 0,
        NondeterministicChoice,
        FairNondeterministicChoice
    }
}
