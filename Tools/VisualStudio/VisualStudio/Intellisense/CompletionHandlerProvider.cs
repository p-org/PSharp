// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# completion handler provider.
    /// </summary>
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("P# completion handler")]
    [ContentType("psharp")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class CompletionHandlerProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        [Import]
        internal Microsoft.VisualStudio.Shell.SVsServiceProvider ServiceProvider { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView != null)
            {
                textView.Properties.GetOrCreateSingletonProperty(() => new CompletionCommandHandler(textViewAdapter, textView, this));
            }
        }
    }
}
