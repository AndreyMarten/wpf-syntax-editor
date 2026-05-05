using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.CodeView;
using SyntaxEditor;
using SyntaxEditor.Models;
using SyntaxEditor.Theming;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SyntaxEditorExample.ViewModels {
	public class MainViewModel : ViewModelBase {

		#region Constants

		string MyLangMonarch = @"{
  defaultToken: ""invalid"",

  keywords: [
    ""function"", ""let"", ""const"", ""if"", ""else"",
    ""return"", ""while"", ""for"", ""true"", ""false"", ""null""
  ],

  typeKeywords: [""number"", ""string"", ""boolean""],

  operators: [
    ""="", "">"", ""<"", ""!"", ""~"", ""?"", "":"", ""=="", ""<="",
    "">="", ""!="", ""&&"", ""||"", ""+"", ""-"", ""*"", ""/"", ""%""
  ],

  symbols: /[=><!~?:&|+\-*\/%]+/,

  escapes: /\\(?:[abfnrtv\\""'`]|x[0-9A-Fa-f]{2}|u[0-9A-Fa-f]{4})/,

  tokenizer: {

    root: [

      // identifiers
      [/[a-zA-Z_$][\w$]*/, {
        cases: {
          ""@keywords"": ""keyword"",
          ""@typeKeywords"": ""type"",
          ""@default"": ""identifier""
        }
      }],

      // whitespace
      { include: ""@whitespace"" },

      // delimiters
      [/[{}()\[\]]/, ""@brackets""],

      // operators
      [/@symbols/, {
        cases: {
          ""@operators"": ""operator"",
          ""@default"": """"
        }
      }],

      // numbers
      [/\d*\.\d+([eE][\-+]?\d+)?/, ""number.float""],
      [/\d+/, ""number""],

      // regex literal (complex rule object)
      {
        regex: /\/(?!\*)(?:[^\\/]|\\.)+\/[gimsuy]*/,
        action: { token: ""regexp"" }
      },

      // strings
      [/""/, { token: ""string.quote"", bracket: ""@open"", next: ""@string"" }],

      // template string
      [/`/, { token: ""string.quote"", bracket: ""@open"", next: ""@template"" }]
    ],

    comment: [
	  [/[^/*]+/, ""comment""],
	  [/\/\*/, ""comment"", ""@push""],
	  [/\*\//, ""comment"", ""@pop""],    
	  [/./, ""comment""]
	],

    string: [
      [/[^\\""]+/, ""string""],
      [/@escapes/, ""string.escape""],
      [/\\./, ""string.escape.invalid""],
      [/""/, { token: ""string.quote"", bracket: ""@close"", next: ""@pop"" }]
    ],

	template: [
	  [/[^\\`$]+/, ""string""],
	  [/\$\{/, { token: ""delimiter.bracket"", next: ""@braced"" }],
	  [/@escapes/, ""string.escape""],
	  [/`/, { token: ""string.quote"", bracket: ""@close"", next: ""@pop"" }]
	],

	braced: [
		[/}/, { token: ""delimiter.bracket"", next: ""@pop"" }],
		{ include: ""@root"" }
	],

    whitespace: [
      [/[ \t\r\n]+/, ""white""],
      [/\/\*/, ""comment"", ""@comment""],
      [/\/\/.*$/, ""comment""]
    ]
  }
}";

		const string MyLangConfiguration = @"{
  comments: {
    lineComment: ""//"",
    blockComment: [""/*"", ""*/""]
  },

  brackets: [
    [""{"", ""}""],
    [""["", ""]""],
    [""("", "")""]
  ],

  autoClosingPairs: [
    { open: ""{"", close: ""}"" },
    { open: ""["", close: ""]"" },
    { open: ""("", close: "")"" },
    { open: ""\"""", close: ""\"""" },
    { open: ""`"", close: ""`"" }
  ],

  surroundingPairs: [
    { open: ""{"", close: ""}"" },
    { open: ""["", close: ""]"" },
    { open: ""("", close: "")"" },
    { open: ""\"""", close: ""\"""" },
    { open: ""`"", close: ""`"" }
  ]
}";

		private const string testText = @"function test(x: number) {

    /* outer comment
        /* nested comment */
    */

    let value = 10.5
    const flag = true

    let regex = /abc\d+/gi

    let str = ""hello world""

    let template = `value is ${value}`

    if (flag && value > 5) {
        return template
    }

    return null
}";

		#endregion Constants

		public MainViewModel() {
			Text = @"/*
* C# Program to Display All the Prime Numbers Between 1 to 100
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VS
{
	class Program
	{
		static void Main(string[] args)
		{
			bool isPrime = true;
			Console.WriteLine(""Prime Numbers : "");
			for (int i = 2; i <= 100; i++)
			{
				for (int j = 2; j <= 100; j++)
				{
					if (i != j && i % j == 0)
					{
						isPrime = false;
						break;
					}
				}
				
				if (isPrime)
				{
					Console.Write(""\t"" +i);
				}
				isPrime = true;
			}
			Console.ReadKey();
		}
	}
}
";

			RefreshLanguagesCommand = new AsyncCommand(RefreshLanguages, CanRefreshLanguages);
		}

		public IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }

		public ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }

		public ISyntaxEditorService SyntaxEditorService { get { return this.GetService<ISyntaxEditorService>(); } }

		public IDialogService DialogService { get { return this.GetService<IDialogService>(); } }

		public string Text {
			get { return this.GetValue<string>(); }
			set { this.SetValue(value); }
		}

		[Command]
		public void OpenFile() {
			if (this.OpenFileDialogService.ShowDialog()) {
				var file = this.OpenFileDialogService.Files.First();
				this.Text = File.ReadAllText(Path.Combine(file.DirectoryName, file.Name));
			}
		}

		public bool CanOpenFile() {
			return OpenFileDialogService != null;
		}

		[Command]
		public void SaveFile() {
			if (this.SaveFileDialogService.ShowDialog()) {
				var file = this.SaveFileDialogService.File;
				File.WriteAllText(Path.Combine(file.DirectoryName, file.Name), this.Text);
				this.SyntaxEditorService.MarkAsSaved();
			}
		}

		public bool CanSaveFile() {
			return this.SaveFileDialogService != null;
		}


		protected ObservableCollection<string> _Languages;
		public ObservableCollection<string> Languages {
			get {
				if (this._Languages == null) {
					this._Languages = new ObservableCollection<string>();
				}

				return this._Languages;
			}
		}

		public IReadOnlyList<MonacoThemeRule> Rules {
			get { return this.GetValue<IReadOnlyList<MonacoThemeRule>>(); }
			set { this.SetValue(value); }
		}

		[Command]
		public async void Initialize() {
			await RefreshLanguages();
			this.Language = this.Languages.Where(c => c.Contains("csharp")).FirstOrDefault();
		}

		public string? Language {
			get { return this.GetValue<string?>(); }
			set { this.SetValue(value); }
		}

		public AsyncCommand RefreshLanguagesCommand { get; private set; }

		private async Task RefreshLanguages() {
			if (this.SyntaxEditorService == null) {
				throw new InvalidOperationException("SyntaxEditorService is not available.");
			}

			this.Languages.Clear();
			var result = await this.SyntaxEditorService.GetLanguagesAsync();
			if (result != null) {
				this.Languages.AddRange(result);
			}
		}
		private bool CanRefreshLanguages() {
			return this.SyntaxEditorService != null;
		}

		[Command]
		public void ChangeRules() {

            if (this.DialogService == null) {
                throw new InvalidOperationException("DialogService is not available.");
            }

			var vm = new RulesViewModel();
            if (this.Rules != null)
                vm.Rules.AddRange(this.Rules);

            var buttonSave = new UICommand() {
                Id = "save",
                Caption = "Save",
                Command = new DelegateCommand(() => { vm.ApplyRulesChanges(); }),
                IsDefault = true,
                IsCancel = false
            };

            var buttonCancel = new UICommand() {
                Id = "cancel",
                Caption = "Cancel",
                Command = new DelegateCommand(() => { }),
                IsDefault = false,
                IsCancel = true
            };

            UICommand result;

            result = DialogService.ShowDialog(
                dialogCommands: new UICommand[] { buttonSave, buttonCancel },
                title: "Change Rules",
                documentType: "RulesView",
                viewModel: vm
            );

            if (result != buttonSave) {
                return;
            }
            //update rules so that theme can apply it.
            this.Rules = vm.Rules.ToList();
        }

		public bool CanChangeRules() {
			return this.DialogService != null;
        }

            [Command]
		public async void RegisterCustomLanguage() {

			if (this.SyntaxEditorService == null) {
				throw new InvalidOperationException("SyntaxEditorService is not available.");
			}

			if(this.DialogService == null) {
				throw new InvalidOperationException("DialogService is not available.");
            }	

			var vm = new CustomLanguageViewModel();
			vm.Monarch = MyLangMonarch;
			vm.Configuration = MyLangConfiguration;
			vm.LanguageId = "MyLang";
			
			
            var buttonSave = new UICommand() {
                Id = "save",
                Caption = "Save",
                Command = new DelegateCommand(() => { }, () => { return !string.IsNullOrWhiteSpace(vm.LanguageId); }),
			    IsDefault = true,
                IsCancel = false
            };
            
            var buttonCancel = new UICommand() {
                Id = "cancel",
                Caption = "Cancel",
                Command = new DelegateCommand(() => { }),
                IsDefault = false,
                IsCancel = true
            };

            UICommand result;

            result = DialogService.ShowDialog(
                dialogCommands: new UICommand[] { buttonSave, buttonCancel },
                title: "Register Custom Language",
				documentType: "CustomLanguageView",
                viewModel: vm
            );

            if (result != buttonSave) {
				return;
            }


            var myLang = new LanguageDescriptor {
				Id = vm.LanguageId,
				Monarch = vm.Monarch,
				Configuration = vm.Configuration
			};

			this.SyntaxEditorService.RegisterLanguage(myLang);
			await this.RefreshLanguages();
			this.Language = vm.LanguageId;

            this.Text = testText;
		}

		public bool CanRegisterCustomLanguage() {
			return this.DialogService != null && this.SyntaxEditorService != null;
        }
	}
}