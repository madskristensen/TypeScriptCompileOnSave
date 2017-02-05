using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Windows.Threading;

namespace TypeScriptCompileOnSave
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("typescript")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class VsTextViewCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        private static DTE2 _dte = VsHelpers.GetService<DTE, DTE2>();

        public void TextViewCreated(IWpfTextView view)
        {
            if (!DocumentService.TryGetTextDocument(view.TextBuffer, out ITextDocument document))
                return;

            ProjectItem item = _dte.Solution.FindProjectItem(document.FilePath);

            if (item == null || item.ContainingProject == null)
                return;

            document.TextBuffer.Properties.AddProperty("item", item);

            view.Properties.AddProperty("doc", document);
            view.Closed += TextViewClosed;
            document.FileActionOccurred += DocumentSaved;
        }

        private async void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType != FileActionTypes.ContentSavedToDisk)
                return;

            var document = (ITextDocument)sender;

            if (!document.TextBuffer.Properties.TryGetProperty("item", out ProjectItem item))
                return;


            TranspilerStatus status = await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    return await item.Transpile();
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    return TranspilerStatus.Exception;
                }
            });
        }

        private void TextViewClosed(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;
            view.Closed -= TextViewClosed;

            if (view.Properties.TryGetProperty("doc", out ITextDocument document))
            {
                document.FileActionOccurred -= DocumentSaved;
            }
        }
    }
}
