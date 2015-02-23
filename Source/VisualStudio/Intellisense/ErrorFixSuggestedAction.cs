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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PSharp.VisualStudio
{
    internal class ErrorFixSuggestedAction : ISuggestedAction
    {
        private ITrackingSpan Span;
        private ITextSnapshot Snapshot;

        private bool IsDisposed;

        public IEnumerable<SuggestedActionSet> ActionSets
        {
            get
            {
                return null;
            }
        }

        public string DisplayText
        {
            get { return "TEST"; }
        }

        public string IconAutomationText
        {
            get
            {
                return null;
            }
        }

        public ImageSource IconSource
        {
            get { return null; }
        }

        public string InputGestureText
        {
            get
            {
                return null;
            }
        }

        public ErrorFixSuggestedAction(ITrackingSpan span)
        {
            this.Span = span;
            this.Snapshot = span.TextBuffer.CurrentSnapshot;
            this.IsDisposed = false;
            //m_upper = span.GetText(m_snapshot).ToUpper();
            //m_display = string.Format("Convert '{0}' to lower case", span.GetText(m_snapshot));
        }

        public object GetPreview(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            textBlock.Inlines.Add(new Run() { Text = "test" });
            return textBlock;
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            this.Span.TextBuffer.Replace(this.Span.GetSpan(this.Snapshot), "X");
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public void Dispose()
        {
            if (!this.IsDisposed)
            {
                GC.SuppressFinalize(this);
                this.IsDisposed = true;
            }
        }
    }
}
