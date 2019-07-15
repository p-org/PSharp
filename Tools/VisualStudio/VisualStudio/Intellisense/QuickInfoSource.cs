using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.PSharp.LanguageServices.Parsing;
using System.Linq;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.PSharp.VisualStudio
{
    internal class PSharpQuickInfoSource : IQuickInfoSource
    {
        private PSharpQuickInfoSourceProvider provider;
        private ITextBuffer subjectBuffer;
        private ITextBuffer csharpBuffer;
        ITagAggregator<PSharpTokenTag> tagAggregator;

        internal static Dictionary<TokenType, string> TokenTypeTips = new Dictionary<TokenType, string>
        {
            // Note: the triple-slash comments are not available via Reflection.
            [TokenType.MachineDecl] = "An abstract class representing a P# state machine.",
            //TODO [TokenType.MachineIdDecl] = "A unique reference to a P# state machine class instance",
            [TokenType.MonitorDecl] = "An abstract class representing a P# monitor.",
            [TokenType.StateDecl] = "A state in a P# state machine.",
            [TokenType.StateGroupDecl] = "A group of states or of other state groups in a P# state machine.",
            [TokenType.EventDecl] = "An event that will be sent from one machine to another machine or raised to itself, usually triggering a state transition",
            [TokenType.StartState] = "The initial state for this P# state machine",
            [TokenType.HotState] = "A liveness monitor state indicating that an operation is required and has not yet occurred",
            [TokenType.ColdState] = "A liveness monitor state indicating that an operation required by a hot state has been completed",
            [TokenType.EventIdentifier] = "An event that will be sent from one machine to another machine or raised to itself",
            [TokenType.MachineIdentifier] = "A P# state machine class definition",
            [TokenType.StateIdentifier] = "A state in a P# state machine",
            [TokenType.StateGroupIdentifier] = "A group of states or of other state groups in a P# state machine",
            [TokenType.ActionIdentifier] = "An action to be performed",
            [TokenType.TypeIdentifier] = "A type in P# code",   // TODO better definition here
            [TokenType.CreateMachine] = "Create an instance of a P# state machine class",
            [TokenType.CreateRemoteMachine] = "Create a remote instance of a P# state machine class",
            [TokenType.SendEvent] = "Send an event from one machine to another",
            [TokenType.RaiseEvent] = "Send an event from this machine to itself",
            [TokenType.Jump] = "Transition this machine to another state",
            [TokenType.Assert] = "Assert that a condition is true",
            [TokenType.Assume] = "Assume that a condition is true",
            [TokenType.PopState] = "Pop a state from the state queue",
            [TokenType.OnAction] = "Specify an event for which an action is to be performed",
            [TokenType.DoAction] = "Specify an action to be performed when an event occurs",
            [TokenType.GotoState] = "Transition this machine to another state",
            [TokenType.PushState] = "Push a state onto the state queue",
            [TokenType.WithExit] = "Perform an additional action on a state transition",
            [TokenType.DeferEvent] = "Defer handling of an event until the machine transitions out of the specified state",
            [TokenType.IgnoreEvent] = "Ignore an event while the machine is in the specified state",
            [TokenType.Entry] = "An action to be executed by a machine on entry to a state",
            [TokenType.Exit] = "An action to be executed by a machine on exit from a state",
            [TokenType.Trigger] = "A reference to the currently received event; may be cast to a specific event type to obtain the event payload, if any",
            [TokenType.HaltEvent] = "Halt the machine; consumes but does not operate on events",
            [TokenType.DefaultEvent] = "An action to be run when there is no event in the queue for this machine state",
            [TokenType.NonDeterministic] = "Return a reproducibly random boolean value"
        };

        private static Dictionary<TokenType, string> IdentifierTokenTypes = new Dictionary<TokenType, string>
        {
            [TokenType.EventIdentifier] = "event",
            [TokenType.MachineIdentifier] = "machine",
            [TokenType.StateIdentifier] = "state",
            [TokenType.StateGroupIdentifier] = "state group",
            [TokenType.ActionIdentifier] = "action",
            [TokenType.TypeIdentifier] = "type",
        };

        internal static HashSet<TokenType> IgnoreTokenTypes = new HashSet<TokenType>
        {
            TokenType.WhiteSpace,
            TokenType.QuotedString,
            TokenType.NewLine,
            TokenType.Comment,
            TokenType.CommentLine,
            TokenType.CommentStart,
            TokenType.CommentEnd
        };

        internal static bool IsIgnoreTokenType(TokenType t) => IgnoreTokenTypes.Contains(t);

        public PSharpQuickInfoSource(PSharpQuickInfoSourceProvider provider, ITextBuffer subjectBuffer, ITagAggregator<PSharpTokenTag> tagAggregator)
        {
            this.provider = provider;
            this.subjectBuffer = subjectBuffer;
            this.tagAggregator = tagAggregator;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;
#if false // TODO ProjectionBuffer
            if (!ProjectionBufferGraph.GetFromProperties(session.TextView.BufferGraph.TopBuffer, out ProjectionBufferGraph projectionBufferGraph))
            {
                return;
            }
#endif

            // If the mouse position maps down to the csharp buffer then it should be handled by C# only.
            this.csharpBuffer = this.csharpBuffer ?? PSharpQuickInfoController.GetCSharpBuffer(session.TextView.BufferGraph.TopBuffer);
            if (this.csharpBuffer != null && session.GetTriggerPoint(this.csharpBuffer.CurrentSnapshot) != null)
            {
                return;
            }

            // Map the trigger point down to our buffer.
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(this.subjectBuffer.CurrentSnapshot);
            if (!subjectTriggerPoint.HasValue)
            {
                return;
            }
#if false // TODO ProjectionBuffer
            var isEmbeddedPSharp = projectionBufferGraph.IsEmbeddedPSharpPoint(subjectTriggerPoint.Value);
#endif

            TokenType preprocessTokenType(TokenType tokenType)
            {
#if false // TODO ProjectionBuffer
                if (isEmbeddedPSharp)
                {
                    switch (tokenType)
                    {
                        case TokenType.MachineDecl:
                            return TokenType.MachineIdDecl;
                    }
                }
#endif
                return tokenType;
            }

            var currentSnapshot = subjectTriggerPoint.Value.Snapshot;
            var querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);

            foreach (var tagSpan in this.tagAggregator.GetTags(new SnapshotSpan(subjectTriggerPoint.Value, subjectTriggerPoint.Value)))
            {
                var tokenType = preprocessTokenType(tagSpan.Tag.Type);
                if (IsIgnoreTokenType(tokenType) || !TokenTypeTips.TryGetValue(tokenType, out string content))
                {
                    continue;
                }
                var firstSpan = tagSpan.Span.GetSpans(this.subjectBuffer).First();
                applicableToSpan = this.subjectBuffer.CurrentSnapshot.CreateTrackingSpan(firstSpan, SpanTrackingMode.EdgeExclusive);

                var start = tagSpan.Span.Start.GetPoint(this.subjectBuffer, PositionAffinity.Predecessor).Value.Position;
                var end = tagSpan.Span.End.GetPoint(this.subjectBuffer, PositionAffinity.Predecessor).Value.Position;
                var text = currentSnapshot.GetText().Substring(start, end - start);
                var prefix = IdentifierTokenTypes.TryGetValue(tokenType, out string identifierTypeName)
                            ? $"({identifierTypeName}) {text}: "
                            : string.Empty;
                qiContent.Add($"{prefix}{content}");
            }
        }

        private bool isDisposed;
        public void Dispose()
        {
            if (!this.isDisposed)
            {
                GC.SuppressFinalize(this);
                this.isDisposed = true;
            }
        }
    }
}
