//-----------------------------------------------------------------------
// <copyright file="CompletionCommandHandler.cs">
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
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# completion command handler.
    /// </summary>
    internal class CompletionCommandHandler : IOleCommandTarget
    {
        private IOleCommandTarget NextCommandHandler;
        private ITextView TextView;
        private CompletionHandlerProvider Provider;
        private ICompletionSession Session;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="textViewAdapter">IVsTextView</param>
        /// <param name="textView">ITextView</param>
        /// <param name="provider">Provider</param>
        internal CompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView,
            CompletionHandlerProvider provider)
        {
            this.TextView = textView;
            this.Provider = provider;

            // Add the command to the command chain
            textViewAdapter.AddCommandFilter(this, out this.NextCommandHandler);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return this.NextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (VsShellUtilities.IsInAutomationFunction(this.Provider.ServiceProvider))
            {
                return this.NextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            // Make sure the input is a char before getting it.
            var typedChar = pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR
                ? (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn)
                : char.MinValue;

            // Check for a commit character.
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB
                || char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar))
            {
                // Check for a selection.
                if (this.Session != null && !this.Session.IsDismissed)
                {
                    // If the selection is fully selected, commit the current session and don't add the character to the buffer.
                    if (this.Session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        this.Session.Commit();
                        return VSConstants.S_OK;
                    }

                    // There is no selection so dismiss the session.
                    this.Session.Dismiss();
                }
            }

            // Pass along the command so the char is added to the buffer.
            int nextCommandRetVal = this.NextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            bool handled = false;

            if (!typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar))
            {
                // If there is no active session, bring up completion before filtering.
                if (this.Session == null || this.Session.IsDismissed)
                {
                    this.TriggerCompletion();
                    if (this.Session == null)
                    {
                        return VSConstants.S_OK;
                    }
                }
                this.Session.Filter();
                handled = true;
            }
            else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE || nCmdID == (uint)VSConstants.VSStd2KCmdID.DELETE)
            {
                // There is a deletion so redo the filter if we have an active session.
                if (this.Session != null && !this.Session.IsDismissed)
                {
                    this.Session.Filter();
                }
                handled = true;
            }

            return handled ? VSConstants.S_OK : nextCommandRetVal;
        }

        private bool TriggerCompletion()
        {
            // The caret must be in a non-projection location.
            SnapshotPoint? caretPoint = this.TextView.Caret.Position.Point.GetPoint(
                textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
            if (!caretPoint.HasValue)
            {
                return false;
            }

            this.Session = this.Provider.CompletionBroker.CreateCompletionSession(this.TextView,
                caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
                true);

            // Subscribe to the Dismissed event on the session.
            this.Session.Dismissed += this.OnSessionDismissed;
            this.Session.Start();

            return true;
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            this.Session.Dismissed -= this.OnSessionDismissed;
            this.Session = null;
        }
    }
}
