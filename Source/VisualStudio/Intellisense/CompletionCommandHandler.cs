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
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

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

            //add the command to the command chain
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

            // Make a copy of this so we can look at it after forwarding some commands.
            uint commandID = nCmdID;
            char typedChar = char.MinValue;

            // Make sure the input is a char before getting it.
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            // Check for a commit character.
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB
                || (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar)))
            {
                // Check for a a selection.
                if (this.Session != null && !this.Session.IsDismissed)
                {
                    // If the selection is fully selected, commit the current session.
                    if (this.Session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        this.Session.Commit();
                        // Also, don't add the character to the buffer.
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        // If there is no selection, dismiss the session.
                        this.Session.Dismiss();
                    }
                }
            }

            // Pass along the command so the char is added to the buffer.
            int retVal = this.NextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            bool handled = false;

            if (!typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar))
            {
                // If there is no active session, bring up completion.
                if (this.Session == null || this.Session.IsDismissed)
                {
                    this.TriggerCompletion();
                    this.Session.Filter();
                }
                // The completion session is already active, so just filter.
                else
                {
                    this.Session.Filter();
                }

                handled = true;
            }
            // Redo the filter if there is a deletion.
            else if (commandID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE
                || commandID == (uint)VSConstants.VSStd2KCmdID.DELETE)
            {
                if (this.Session != null && !this.Session.IsDismissed)
                {
                    this.Session.Filter();
                }

                handled = true;
            }

            if (handled)
            {
                return VSConstants.S_OK;
            }

            return retVal;
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
