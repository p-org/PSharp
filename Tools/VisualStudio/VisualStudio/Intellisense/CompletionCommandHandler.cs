// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

// For Snippet expansion
using Microsoft.VisualStudio.Text.Operations;
using MSXML;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using System.Collections;
using System.Runtime.InteropServices;

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
    /// but the VSIX build process does not appear to pick up the snippet files from from %InstallRoot%. Distribution of snippets without
    /// <see cref="ProvideLanguageCodeExpansionAttribute"/> (instead creating the pkgdef manually) is described in 
    /// https://docs.microsoft.com/en-us/visualstudio/ide/how-to-distribute-code-snippets?view=vs-2019. The approach used below is a hybrid of 
    /// these, using <see cref="ProvideLanguageCodeExpansionAttribute"/> but based upon $PackageFolder$ instead of %InstallRoot% (note that the
    /// $ vs % is significant).
    /// </remarks>
    [ProvideLanguageCodeExpansion(
        SnippetUtilities.LanguageGuidStr,       // Guid from SnippetsIndex.xml
        "PSharp",                               // Language name from SnippetsIndex.xml
        0,                                      // Resource id of the language
        "PSharp",                               // Language ID used in the .snippet files
        @"$PackageFolder$%\Snippets\%LCID%\SnippetsIndex.xml", // Path of the index file
        SearchPaths = @"$PackageFolder$\Snippets\%LCID%\PSharp",
        ForceCreateDirs = @"$PackageFolder$\Snippets\%LCID%\PSharp")]
    internal class CompletionCommandHandler : IOleCommandTarget, IVsExpansionClient
    {
        private IOleCommandTarget NextCommandHandler;
        private ITextView TextView;
        private CompletionHandlerProvider Provider;
        private ICompletionSession Session;

        private bool outliningIsStopped; // Whether the "Stop Outlining" Edit->Outlining option has been selected

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

            // Get the text manager from the service provider
            var textManager = (IVsTextManager2)this.Provider.ServiceProvider.GetService(typeof(SVsTextManager));
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
                    // make the Insert Snippet, CommentBlock, and UncommentBlock command appear on the context menu
                    if ((uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET
                        || (uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.COMMENTBLOCK
                        || (uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.COMMENT_BLOCK
                        || (uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK
                        || (uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK
                        || (uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.OUTLN_TOGGLE_ALL
                        || (uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.OUTLN_TOGGLE_CURRENT
                        // "HIDING" means start/stop outlining for these two
                        || (!this.outliningIsStopped && (uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.OUTLN_STOP_HIDING_ALL)
                        || (this.outliningIsStopped && (uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.OUTLN_START_AUTOHIDING)
                        )
                    {
                        prgCmds[0].cmdf = (int)Constants.MSOCMDF_ENABLED | (int)Constants.MSOCMDF_SUPPORTED;
                        return VSConstants.S_OK;
                    }

                    if ((uint)prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.OUTLN_STOP_HIDING_CURRENT)
                    {
                        return (int)Constants.MSOCMDERR_E_DISABLED;
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

            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.OUTLN_STOP_HIDING_ALL
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.OUTLN_START_AUTOHIDING)
            {
                var hr = this.NextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                if (hr == VSConstants.S_OK)
                {
                    this.outliningIsStopped = !this.outliningIsStopped;
                }
                return hr;
            }

            // Make sure the input is a char before getting it.
            var typedChar = pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR && pvaIn != IntPtr.Zero
                ? (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn)
                : char.MinValue;

#if false // TODO: Requires NotYetImplemented ProjectionTree for performance

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
                return VSConstants.S_OK;
            }
            
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE || nCmdID == (uint)VSConstants.VSStd2KCmdID.DELETE)
            {
                // There is a deletion so redo the filter if we have an active session.
                if (this.Session != null && !this.Session.IsDismissed)
                {
                    this.Session.Filter();
                }
                return VSConstants.S_OK;
            }
#endif

            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
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
                        var hr = this.ExpansionSession.GoToPreviousExpansionField();
                        return VSConstants.S_OK;
                    }
                    else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
                    {
                        var hr = this.ExpansionSession.GoToNextExpansionField(0); //false to support cycling through all the fields
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

                // See if we are commenting or uncommenting a selection.
                if (nCmdID == (uint)VSConstants.VSStd2KCmdID.COMMENTBLOCK
                    || nCmdID == (uint)VSConstants.VSStd2KCmdID.COMMENT_BLOCK)
                {
                    return ToggleSelectionComments(true);
                }
                if (nCmdID == (uint)VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK
                    || nCmdID == (uint)VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK)
                {
                    return ToggleSelectionComments(false);
                }
            }

#if true    // TODO: NotYetImplemented ProjectionTree will remove this in favor of calling it above (in the #ifdef'd part)
            // Pass along the command so the char is added to the buffer.
            int nextCommandRetVal = this.NextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
#endif

            // Indenting is not triggered on typing '}' so do it ourselves
            if (nextCommandRetVal == VSConstants.S_OK && typedChar == '}')
            {
                var line = this.TextView.Caret.Position.BufferPosition.GetContainingLine();
                var span = new TextSpan
                {
                    iStartLine = line.LineNumber,
                    iStartIndex = line.Start.Position,
                    iEndLine = line.LineNumber,
                    iEndIndex = line.Start.Position + 1
                };
                nextCommandRetVal = FormatSpan(new[] { span });
            }

            return nextCommandRetVal;
        }

        // begin for snippets only...
        public int EndExpansion()
        {
            this.ExpansionSession = null;
            return VSConstants.S_OK;
        }

        public int FormatSpan(IVsTextLines textLines, TextSpan[] ts) => FormatSpan(ts);

        private int FormatSpan(TextSpan[] ts)
        {
            // This is for snippets so take only the first span, and replace only the indent so the field markers are preserved.
            var span = ts[0];
            var indentReplacements = new Indent(this.TextView).GetSpanIndents(span, this.TextView.Options.GetIndentSize()).ToArray();

            using (var bufferEdit = this.TextView.TextBuffer.CreateEdit())
            {
                for (var lineNum = span.iStartLine; lineNum <= span.iEndLine; ++lineNum)
                {
                    var line = this.TextView.TextSnapshot.GetLineFromLineNumber(lineNum);
                    var indentReplacement = indentReplacements[lineNum - span.iStartLine];
                    bufferEdit.Replace(line.Start.Position, indentReplacement.ExistingIndentLength, indentReplacement.NewIndent);
                }
                bufferEdit.Apply();
            }
            return VSConstants.S_OK;
        }

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
                    int hr = bufferExpansion.InsertNamedExpansion(title, path, addSpan, this, new Guid(SnippetUtilities.LanguageGuidStr),
                                                                  1 /* showDisambiguationUI = false */, out this.ExpansionSession);
                    if (hr == VSConstants.S_OK)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // ... end for snippets only

        public int ToggleSelectionComments(bool addComments)
        {
            var startLine = this.TextView.Selection.Start.Position.GetContainingLine();
            var endLine = this.TextView.Selection.End.Position.GetContainingLine();
            var endLineNumber = endLine.LineNumber;
            var hasSelection = this.TextView.Selection.End.Position.Position > this.TextView.Selection.Start.Position.Position;

            // If selection ends at the start of the next line, we'll mistakenly include that line as well.
            if (hasSelection && endLineNumber > startLine.LineNumber && this.TextView.Selection.End.Position.Position == endLine.Start.Position)
            {
                endLine = startLine.Snapshot.GetLineFromLineNumber(--endLineNumber);
            }

        	var hr = addComments ? this.CommentSelection(startLine, endLine) : this.UncommentSelection(startLine, endLine);
            if (hr != VSConstants.S_OK)
            {
                return hr;
            }

            if (hasSelection)
            {
                // There was selection before so restore it. Re-get start and end lines as their snapshot has changed.
                startLine = this.TextView.Selection.Start.Position.GetContainingLine();
                endLine = startLine.Snapshot.GetLineFromLineNumber(endLineNumber);
                this.TextView.Selection.Select(new SnapshotSpan(startLine.Start, endLine.End), false);
            }
            return VSConstants.S_OK;
        }

        private int CommentSelection(ITextSnapshotLine startLine, ITextSnapshotLine endLine)
        {
            // Two passes; one to get the lowest first non-blank character index on any line,
            // and then one to insert RegionParser.LineComment at that position.
            int lowestFirstNonBlankChar = int.MaxValue;
    
            IEnumerable<ITextSnapshotLine> getLines()
            {
                for (int ii = startLine.LineNumber; ii <= endLine.LineNumber; ii++)
                {
                    var line = startLine.Snapshot.GetLineFromLineNumber(ii);
                    string text = line.GetText();
                    for (var jj = 0; jj < text.Length; ++jj)
                    {
                        if (!char.IsWhiteSpace(text[jj]))
                        {
                            // Do not check to see if there is already a comment there; C# doesn't.
                            if (jj < lowestFirstNonBlankChar)
                            {
                                lowestFirstNonBlankChar = jj; 
                            }
                            yield return line;
                            break;
                        }
                    }
                }
            }

            var lines = getLines().ToArray();   // ToArray() forces iteration and thus execution
            if (lowestFirstNonBlankChar == int.MaxValue)
            {
                return VSConstants.S_FALSE;
            }

            using (var textEdit = startLine.Snapshot.TextBuffer.CreateEdit())
            {
                Array.ForEach(lines, line => textEdit.Insert(line.Start.Position + lowestFirstNonBlankChar, RegionParser.LineComment));
                textEdit.Apply();
            }
            return VSConstants.S_OK;
        }

        private int UncommentSelection(ITextSnapshotLine startLine, ITextSnapshotLine endLine)
        {
            int hr = VSConstants.S_FALSE;
            using (var textEdit = startLine.Snapshot.TextBuffer.CreateEdit())
            {
                for (var ii = startLine.LineNumber; ii <= endLine.LineNumber; ++ii)
                {
                    var line = startLine.Snapshot.GetLineFromLineNumber(ii);
                    var text = line.GetTextIncludingLineBreak();
                    var commentIndex = text.IndexOf(RegionParser.LineComment);
                    if (commentIndex >= 0)
                    {
                        textEdit.Delete(line.Start.Position + commentIndex, RegionParser.LineComment.Length);
                        hr = VSConstants.S_OK;
                    }
                }

                if (hr == VSConstants.S_OK)
                {
                    textEdit.Apply(); 
                }
            }
            return hr;
        }

#if false // TODO: Requires NotYetImplemented ProjectionTree for performance

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
