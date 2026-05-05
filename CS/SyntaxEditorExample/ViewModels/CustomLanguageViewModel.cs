using DevExpress.Mvvm;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace SyntaxEditorExample.ViewModels {
    public class CustomLanguageViewModel : ViewModelBase, IDataErrorInfo {
        public string LanguageId {
            get { return this.GetValue<string>(); }
            set { this.SetValue(value); }
        }

        public string? Monarch {
            get { return this.GetValue<string?>(); }
            set { this.SetValue(value); }
        }

        public string? Configuration {
            get { return this.GetValue<string?>(); }
            set { this.SetValue(value); }
        }

        public string? Error => null;

        private static readonly Regex LanguageIdRegex = new("^[a-zA-Z0-9\\-_]+$", RegexOptions.Compiled);

        public string? this[string columnName] {
            get {
                if (columnName == nameof(LanguageId)) {
                    if (string.IsNullOrWhiteSpace(LanguageId))
                        return "Language Name is required.";

                    if (!LanguageIdRegex.IsMatch(LanguageId))
                        return "Language Name may contain only letters, digits, '-' and '_'.";

                    if (LanguageId.Length > 50)
                        return "Language Name is too long.";
                }

                return null;
            }
        }
    }
}
