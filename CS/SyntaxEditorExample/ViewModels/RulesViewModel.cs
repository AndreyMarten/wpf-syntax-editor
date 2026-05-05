using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
using DevExpress.Xpf.CodeView;
using SyntaxEditor.Theming;
using SyntaxEditorExample.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SyntaxEditorExample.ViewModels {
    public class RulesViewModel : ViewModelBase {

        public IMessageBoxService MessageBoxService => this.GetService<IMessageBoxService>();

        protected ObservableCollection<MonacoThemeRule> _Rules;
        public ObservableCollection<MonacoThemeRule> Rules {
            get {
                if (this._Rules == null) {
                    this._Rules = new ObservableCollection<MonacoThemeRule>();
                }

                return this._Rules;
            }
        }

        public string? RawRulesText {
            get { return this.GetValue<string?>(); }
            set { this.SetValue(value); }
        }

        public bool IsRawRulesMode {
            get { return this.GetValue<bool>(); }
            set { this.SetValue(value); }
        }

        private void ParseRawJS() {
            
        }

        [Command]
        public void ApplyRules() {
            RawRulesText = MonacoRulesParser.Serialize(Rules);
        }

        [Command]
        public void ApplyJS() {
            if (!MonacoRulesParser.TryParse(RawRulesText ?? string.Empty, out var parsed)) {
                MessageBoxService?.ShowMessage(
                    "Failed to parse rules. Please check the format.",
                    "Error",
                    MessageButton.OK,
                    MessageIcon.Error);

                this.IsRawRulesMode = true;
            }

            Rules.Clear();
            Rules.AddRange(parsed);
        }


        [Command]
        public void ApplyRulesChanges() {
            if (IsRawRulesMode) {
                ApplyJS();
            } else {
                ApplyRules();
            }
        }

        [Command]
        public void AddingNewRule(NewRowArgs args) {
            args.Item = new MonacoThemeRule() {
                Token = "new-token"
            };
        }

        [Command]
        public void Initialized() {
            ApplyRulesChanges();
        }
    }
}
