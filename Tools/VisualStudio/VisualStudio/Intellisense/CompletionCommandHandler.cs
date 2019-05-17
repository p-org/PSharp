// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

// For Snippet expansion
using Microsoft.VisualStudio.Text.Operations;
using MSXML;
using System.ComponentModel.Composition;

namespace Microsoft.PSharp.VisualStudio
{
    static class SnippetUtilities
    {
        internal const string LanguageGuidStr = "DEE9105E-DD6A-4950-87B9-1435A6668226"; // Guid from SnippetsIndex.xml
    }
    /// <summary>
    /// The P# completion command handler. As it is an IOleCommandTarget we also implement CodeExpansion in it.
    /// </summary>
    /// <remarks>
    /// Current source for CodeExpansion is https://docs.microsoft.com/en-us/visualstudio/extensibility/walkthrough-implementing-code-snippets?view=vs-2019
    /// </remarks>
    [ProvideLanguageCodeExpansion(
        SnippetUtilities.LanguageGuidStr,       // Guid from SnippetsIndex.xml
        "PSharp",                               // Language name from SnippetsIndex.xml
        0,                                      // Resource id of the language
        "PSharp",                               // Language ID used in the .snippet files
        @"%InstallRoot%\P#\Snippets\%LCID%\SnippetsIndex.xml", // Path of the index file
        SearchPaths = @"%InstallRoot%\P#\Snippets\%LCID%\PSharp",
        ForceCreateDirs = @"%InstallRoot%\P#\Snippets\%LCID%\PSharp")]
    internal class CompletionCommandHandler : IOleCommandTarget, IVsExpansionClient
    {
        private IOleCommandTarget NextCommandHandler;
        private ITextView TextView;
        private CompletionHandlerProvider Provider;
        private ICompletionSession Session;

        // For Snippet expansion
        IVsTextView VsTextView;
        IVsExpansionManager ExpansionManager;
        IVsExpansionSession ExpansionSession;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="textViewAdapter">IVsTextView</param>
        /// <param name="textView">ITextView</param>
        /// <param name="provider">Provider</param>
        internal CompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, CompletionHandlerProvider provider)
        {
            this.TextView = textView;
            this.VsTextView = textViewAdapter;
            this.Provider = provider;

            //get the text manager from the service provider
            IVsTextManager2 textManager = (IVsTextManager2)this.Provider.ServiceProvider.GetService(typeof(SVsTextManager));
            textManager.GetExpansionManager(out this.ExpansionManager);
            this.ExpansionSession = null;

            // Add the command to the command chain
            textViewAdapter.AddCommandFilter(this, out this.NextCommandHandler);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (!VsShellUtilities.IsInAutomationFunction(this.Provider.ServiceProvider))
            {
                if (pguidCmdGroup == VSConstants.VSStd2K && cCmds > 0)
                {
                    // make the Insert Snippet command appear on the context menu 
                    if ((uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET)
                    {
                        prgCmds[0].cmdf = (int)Constants.MSOCMDF_ENABLED | (int)Constants.MSOCMDF_SUPPORTED;
                        return VSConstants.S_OK;
                    }
                }
            }
            return this.NextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (VsShellUtilities.IsInAutomationFunction(this.Provider.ServiceProvider))
            {
                return this.NextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

#if false // TODO: Statement completion requires NotYetImplemented ProjectionTree so we don't try to apply P# operations in C# blocks.
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
#endif

            // Snippet picker code starts here
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET)
            {
                var textManager = (IVsTextManager2)this.Provider.ServiceProvider.GetService(typeof(SVsTextManager));

                textManager.GetExpansionManager(out this.ExpansionManager);

                int hr = this.ExpansionManager.InvokeInsertionUI(
                    this.VsTextView,
                    this,      //the expansion client
                    new Guid(SnippetUtilities.LanguageGuidStr),
                    null,       //use all snippet types
                    0,          //number of types (0 for all)
                    0,          //ignored if iCountTypes == 0
                    null,       //use all snippet kinds
                    0,          //use all snippet kinds
                    0,          //ignored if iCountTypes == 0
                    "PSharp",   //the text to show in the prompt
                    string.Empty);  //only the ENTER key causes insert 

                return VSConstants.S_OK;
            }

            // The expansion insertion is handled in OnItemChosen; handle navigation if the expansion session is still active
            if (this.ExpansionSession != null)
            {
                if (nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKTAB)
                {
                    this.ExpansionSession.GoToPreviousExpansionField();
                    return VSConstants.S_OK;
                }
                else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
                {
                    this.ExpansionSession.GoToNextExpansionField(0); //false to support cycling through all the fields
                    return VSConstants.S_OK;
                }
                else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN || nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
                {
                    if (this.ExpansionSession.EndCurrentExpansion(0) == VSConstants.S_OK)
                    {
                        return this.EndExpansion();
                    }
                }
            }

            // Neither an expansion session nor a completion session is open, but we got a tab, so check whether the last word typed is a snippet shortcut 
            if (this.Session == null && this.ExpansionSession == null && nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
            {
                //get the word that was just added 
                CaretPosition pos = this.TextView.Caret.Position;
                TextExtent word = this.Provider.NavigatorService.GetTextStructureNavigator(this.TextView.TextBuffer).GetExtentOfWord(pos.BufferPosition - 1); //use the position 1 space back
                string textString = word.Span.GetText(); // The word that was just added; if it is a code snippet, insert it, otherwise carry on
                if (this.InsertAnyExpansion(textString, null, null))
                {
                    return VSConstants.S_OK;
                }
            }

            // Pass along the command so the char is added to the buffer.
            int nextCommandRetVal = this.NextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            return nextCommandRetVal;
        }

        // begin for snippets only...
        public int EndExpansion()
        {
            this.ExpansionSession = null;
            return VSConstants.S_OK;
        }

        public int FormatSpan(IVsTextLines pBuffer, TextSpan[] ts) => VSConstants.S_OK;

        public int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
        {
            pFunc = null;
            return VSConstants.S_OK;
        }

        public int IsValidKind(IVsTextLines pBuffer, TextSpan[] ts, string bstrKind, out int pfIsValidKind)
        {
            pfIsValidKind = 1;
            return VSConstants.S_OK;
        }

        public int IsValidType(IVsTextLines pBuffer, TextSpan[] ts, string[] rgTypes, int iCountTypes, out int pfIsValidType)
        {
            pfIsValidType = 1;
            return VSConstants.S_OK;
        }

        public int OnAfterInsertion(IVsExpansionSession pSession) => VSConstants.S_OK;

        public int OnBeforeInsertion(IVsExpansionSession pSession) => VSConstants.S_OK;

        public int PositionCaretForEditing(IVsTextLines pBuffer, TextSpan[] ts) => VSConstants.S_OK;

        public int OnItemChosen(string pszTitle, string pszPath)
        {
            this.InsertAnyExpansion(null, pszTitle, pszPath);
            return VSConstants.S_OK;
        }

        // This is called with either non-null shortcut and null title/path, or vice-versa.
        private bool InsertAnyExpansion(string shortcut, string title, string path)
        {
            // First get the location of the caret from the IVsTextView, not the ITextView, and set up a TextSpan.
            this.VsTextView.GetCaretPos(out int startLine, out int endColumn);

            TextSpan addSpan = new TextSpan
            {
                iStartIndex = endColumn,
                iEndIndex = endColumn,
                iStartLine = startLine,
                iEndLine = startLine
            };

            if (!string.IsNullOrWhiteSpace(shortcut)) // Get the expansion from the shortcut
            {
                // Reset the TextSpan to the width of the shortcut, because we're going to replace the shortcut with the expansion
                addSpan.iStartIndex = addSpan.iEndIndex - shortcut.Length;

                int hr = this.ExpansionManager.GetExpansionByShortcut(this, new Guid(SnippetUtilities.LanguageGuidStr),
                    shortcut, this.VsTextView, new TextSpan[] { addSpan }, 0, out path, out title);
                if (hr != VSConstants.S_OK)
                {
                    return false;
                }
            }

            if (title != null && path != null)
            {
                if (this.VsTextView.GetBuffer(out IVsTextLines textLines) == VSConstants.S_OK && textLines is IVsExpansion bufferExpansion)
                {
                    int hr = bufferExpansion.InsertNamedExpansion(title, path, addSpan, this,
                        new Guid(SnippetUtilities.LanguageGuidStr), 0, out this.ExpansionSession);
                    if (hr == VSConstants.S_OK)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // ... end for snippets only

#if false // TODO: Statement completion requires NotYetImplemented ProjectionTree so we don't try to apply P# operations in C# blocks.

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
#endif
    }
}
