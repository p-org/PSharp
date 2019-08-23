// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PSharp.VisualStudio
{
#if false // TODO: Requires NotYetImplemented ProjectionTree for performance
    internal class ErrorFixSuggestedAction : ISuggestedAction
    {
        private ITrackingSpan trackingSpan;
        private ITextSnapshot snapshot;
        private string word;
        private KeyValuePair<string, string> replacement;

        private bool isDisposed;

        public string DisplayText => $"Change '{this.word}' to '{this.replacement.Key}'.";

        public bool HasActionSets => false;

        public bool HasPreview => true;

        public string IconAutomationText => null;

        public ImageMoniker IconMoniker => new ImageMoniker();

        public string InputGestureText => null;

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                GC.SuppressFinalize(this);
                this.isDisposed = true;
            }
        }

        public ErrorFixSuggestedAction(string word, KeyValuePair<string, string> replacement, ITrackingSpan trackingSpan)
        {
            this.word = word;
            this.replacement = replacement;
            this.trackingSpan = trackingSpan;
            this.snapshot = trackingSpan.TextBuffer.CurrentSnapshot;
            this.isDisposed = false;
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock { Padding = new Thickness(5) };
            textBlock.Inlines.Add(new Run() { Text = this.replacement.Value });
            return Task.FromResult<object>(textBlock);
        }

        public void Invoke(CancellationToken cancellationToken)
            => this.trackingSpan.TextBuffer.Replace(this.trackingSpan.GetSpan(this.snapshot), this.replacement.Key);

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
#endif
}
