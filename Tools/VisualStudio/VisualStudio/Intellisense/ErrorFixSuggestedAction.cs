// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PSharp.VisualStudio
{
    internal class ErrorFixSuggestedAction : ISuggestedAction
    {
        private ITrackingSpan Span;
        private ITextSnapshot Snapshot;

        private bool IsDisposed;

        public string DisplayText
        {
            get
            {
                return "TEST";
            }
        }

        public bool HasActionSets
        {
            get
            {
                return false;
            }
        }

        public bool HasPreview
        {
            get
            {
                return true;
            }
        }

        public string IconAutomationText
        {
            get
            {
                return null;
            }
        }

        public ImageMoniker IconMoniker
        {
            get
            {
                return new ImageMoniker();
            }
        }

        public string InputGestureText
        {
            get
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (!this.IsDisposed)
            {
                GC.SuppressFinalize(this);
                this.IsDisposed = true;
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

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            textBlock.Inlines.Add(new Run() { Text = "test" });
            return Task.FromResult<object>(textBlock);
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
    }

}
