//-----------------------------------------------------------------------
// <copyright file="ErrorFixSuggestedAction.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
    internal class ErrorFixSuggestedAction : ISuggestedAction
    {
        private ITrackingSpan trackingSpan;
        private ITextSnapshot snapshot;
        private string word;
        private KeyValuePair<string, string> replacement;

        private bool isDisposed;

        public string DisplayText { get { return $"Change '{this.word}' to '{this.replacement.Key}'."; } }

        public bool HasActionSets { get { return false; } }

        public bool HasPreview { get { return true; } }

        public string IconAutomationText { get { return null; } }

        public ImageMoniker IconMoniker { get { return new ImageMoniker(); } }

        public string InputGestureText { get { return null; } }

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
        {
            throw new NotImplementedException();
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock { Padding = new Thickness(5) };
            textBlock.Inlines.Add(new Run() { Text = this.replacement.Value });
            return Task.FromResult<object>(textBlock);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            this.trackingSpan.TextBuffer.Replace(this.trackingSpan.GetSpan(this.snapshot), this.replacement.Key);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

}
