using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace TypeScriptCompileOnSave
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("typescript")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class VsTextViewCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        public async void TextViewCreated(IWpfTextView textView)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                InitializeCompiler(textView);
            });
        }

        private void InitializeCompiler(IWpfTextView textView)
        {
            if (!DocumentService.TryGetTextDocument(textView.TextBuffer, out ITextDocument document))
                return;

            if (!Compiler.CanCompile(document.FilePath, out string cwd))
                return;

            document.FileActionOccurred += (s, e) =>
            {
                if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
                {
                    Compiler.Compile(cwd);
                }
            };
        }
    }
}
