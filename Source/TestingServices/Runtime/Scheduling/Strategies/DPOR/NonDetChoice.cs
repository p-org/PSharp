using
namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies.DPOR
{
    /// <summary>
    /// Stores the outcome of a nondetereminstic (nondet) choice.
    /// </summary>
    internal struct NonDetChoice
    {
        /// <summary>
        /// Is this nondet choice a boolean choice?
        /// If so, <see cref="Choice"/> is 0 or 1.
        /// Otherwise, it can be any int value.
        /// </summary>
        public bool IsBoolChoice;

        /// <summary>
        /// The nondet choice; 0 or 1 if this is a bool choice;
        /// otherwise, any int.
        /// </summary>
        public int Choice;
    }
}
