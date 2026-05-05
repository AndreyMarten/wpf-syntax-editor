using DevExpress.Mvvm.UI;
using SyntaxEditor.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyntaxEditor {
    public interface ISyntaxEditorService {
        void MarkAsSaved();
        Task<IReadOnlyCollection<string>> GetLanguagesAsync(CancellationToken cancellationToken = default);
        void RegisterLanguage(LanguageDescriptor language);
    }

    public class SyntaxEditorService : ServiceBase, ISyntaxEditorService {
        public async Task<IReadOnlyCollection<string>> GetLanguagesAsync(CancellationToken cancellationToken = default) {
            if (this.AssociatedObject is not SyntaxEditor editor)
                throw new InvalidOperationException(
                    "SyntaxEditor is not attached.");

            return await editor.GetAvailableLanguagesAsync();
        }

        public void MarkAsSaved() {
            if(this.AssociatedObject is SyntaxEditor editor) {
                editor.MarkAsSaved();
            }
        }

        public void RegisterLanguage(LanguageDescriptor language) {
            if (this.AssociatedObject is SyntaxEditor editor) {
                editor.RegisterLanguage(language);
            }
        }
    }
}
