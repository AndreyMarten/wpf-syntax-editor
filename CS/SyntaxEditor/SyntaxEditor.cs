using DevExpress.Mvvm;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SyntaxEditor.Models;
using SyntaxEditor.Theming;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SyntaxEditor {
    public class SyntaxEditor : Control, IDisposable {
        WebView2? webView;
        bool editorReady;
        bool updatingFromEditor;
        // Cache for registered languages, to restore when webview2 is recreated.
        // Note: This is a simple cache and does not handle updates to existing languages or removal of languages.
        readonly Dictionary<string, LanguageDescriptor> registeredLanguages = new();
        bool disposed;
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SyntaxEditor), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));
        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(nameof(ReadOnly), typeof(bool), typeof(SyntaxEditor), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnReadOnlyChanged));
        public static readonly DependencyPropertyKey IsModifiedPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsModified), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(false));
        public static readonly DependencyProperty IsModifiedProperty = IsModifiedPropertyKey.DependencyProperty;
        public static readonly DependencyProperty ShowLineNumbersProperty =
            DependencyProperty.Register(nameof(ShowLineNumbers), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnShowLineNumbersChanged));
        public static readonly DependencyProperty ShowMinimapProperty =
            DependencyProperty.Register(nameof(ShowMinimap), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(false, OnShowMinimapChanged));
        public static readonly DependencyProperty ShowGlyphMarginProperty =
            DependencyProperty.Register(nameof(ShowGlyphMargin), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(false, OnShowGlyphMarginChanged));
        public static readonly DependencyProperty EnableFoldingProperty =
            DependencyProperty.Register(nameof(EnableFolding), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableFoldingChanged));
        public static readonly DependencyProperty EnableContextMenuProperty =
            DependencyProperty.Register(nameof(EnableContextMenu), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableContextMenuChanged));
        public static readonly DependencyProperty EnableSmoothScrollingProperty =
            DependencyProperty.Register(nameof(EnableSmoothScrolling), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(false, OnEnableSmoothScrollingChanged));
        public static readonly DependencyProperty EnableScrollBeyondLastLineProperty =
            DependencyProperty.Register(nameof(EnableScrollBeyondLastLine), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableScrollBeyondLastLineChanged));
        public static readonly DependencyProperty ScrollBeyondLastColumnProperty =
            DependencyProperty.Register(nameof(ScrollBeyondLastColumn), typeof(int), typeof(SyntaxEditor), new PropertyMetadata(5, ScrollBeyondLastColumnChanged));
        public static readonly DependencyProperty LineNumbersMinCharsProperty =
            DependencyProperty.Register(nameof(LineNumbersMinChars), typeof(int), typeof(SyntaxEditor), new PropertyMetadata(5, OnLineNumbersMinCharsChanged));
        public static readonly DependencyProperty EnableDragAndDropProperty =
            DependencyProperty.Register(nameof(EnableDragAndDrop), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableDragAndDropChanged));
        public static readonly DependencyProperty EnableMouseWheelZoomProperty =
            DependencyProperty.Register(nameof(EnableMouseWheelZoom), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(false, EnableMouseWheelZoomChanged));
        public static readonly DependencyProperty WordWrapProperty =
            DependencyProperty.Register(nameof(WordWrap), typeof(EditorWordWrap), typeof(SyntaxEditor), new PropertyMetadata(EditorWordWrap.Off, WordWrapChanged));
        public static readonly DependencyProperty EnableStickyScrollProperty =
            DependencyProperty.Register(nameof(EnableStickyScroll), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableStickyScrollChanged));
        public static readonly DependencyProperty TabSizeProperty =
            DependencyProperty.Register(nameof(TabSize), typeof(int), typeof(SyntaxEditor), new PropertyMetadata(4, OnTabSizeChanged), ValidateTabSize);
        public static readonly DependencyProperty DetectIndentationProperty =
            DependencyProperty.Register(nameof(DetectIndentation), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnDetectIndentationChanged));
        public static readonly DependencyProperty InsertSpacesProperty =
            DependencyProperty.Register(nameof(InsertSpaces), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnInsertSpacesChanged));
        public static readonly DependencyProperty AutoIndentProperty =
            DependencyProperty.Register(nameof(AutoIndent), typeof(EditorAutoIndent), typeof(SyntaxEditor), new PropertyMetadata(EditorAutoIndent.Full, AutoIndentChanged));
        public static readonly DependencyProperty EnableQuickSuggestionsProperty =
            DependencyProperty.Register(nameof(EnableQuickSuggestions), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableQuickSuggestionsChanged));
        public static readonly DependencyProperty EnableWordBasedSuggestionsProperty =
            DependencyProperty.Register(nameof(EnableWordBasedSuggestions), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableWordBasedSuggestionsChanged));
        public static readonly DependencyProperty EnableSuggestOnTriggerCharactersProperty =
            DependencyProperty.Register(nameof(EnableSuggestOnTriggerCharacters), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableSuggestOnTriggerCharactersChanged));
        public static readonly DependencyProperty EnableParameterHintsProperty =
            DependencyProperty.Register(nameof(EnableParameterHints), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableParameterHintsChanged));
        public static readonly DependencyProperty ThemeNameProperty =
            DependencyProperty.Register(nameof(ThemeName), typeof(string), typeof(SyntaxEditor), new PropertyMetadata("vs", OnThemeNameChanged));
        public static readonly DependencyProperty EditorLanguageProperty =
            DependencyProperty.Register(nameof(EditorLanguage), typeof(string), typeof(SyntaxEditor), new FrameworkPropertyMetadata("csharp", OnEditorLanguageChanged));
        event EventHandler<IReadOnlyList<string>>? LanguagesReceivedInternal;

        static SyntaxEditor() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SyntaxEditor), new FrameworkPropertyMetadata(typeof(SyntaxEditor)));
        }

        public SyntaxEditor() {
            MarkAsSavedCommand = new DelegateCommand(MarkAsSaved);
        }


        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        static void OnTextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;

            if (control.updatingFromEditor)
                return;

            var text = e.NewValue as string ?? string.Empty;
            control.SetEditorText(text);
        }

        void SetEditorText(string text) {
            this.SendCommand(EditorCommandType.SetText, text);
        }

        public void MarkAsSaved() {
            this.SendCommand(EditorCommandType.MarkAsSaved);
        }

        public ICommand MarkAsSavedCommand { get; private set; }

        public bool ReadOnly {
            get { return (bool)GetValue(ReadOnlyProperty); }
            set { SetValue(ReadOnlyProperty, value); }
        }

        static void OnReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEditorReadOnly((bool)e.NewValue);
        }

        void SetEditorReadOnly(bool readOnly) {
            this.SendCommand(EditorCommandType.SetReadOnly, readOnly);
        }

        public bool IsModified {
            get => (bool)GetValue(IsModifiedProperty);
            private set => SetValue(IsModifiedPropertyKey, value);
        }


        static string ToMonacoOption(EditorOption option) => option switch {
            EditorOption.LineNumbers => "lineNumbers",
            EditorOption.Minimap => "minimap",
            EditorOption.GlyphMargin => "glyphMargin",
            EditorOption.Folding => "folding",
            EditorOption.ScrollBeyondLastLine => "scrollBeyondLastLine",
            EditorOption.ScrollBeyondLastColumn => "scrollBeyondLastColumn",
            EditorOption.ContextMenu => "contextmenu",
            EditorOption.SmoothScrolling => "smoothScrolling",
            EditorOption.DragAndDrop => "dragAndDrop",
            EditorOption.MouseWheelZoom => "mouseWheelZoom",
            EditorOption.LineNumbersMinChars => "lineNumbersMinChars",
            EditorOption.WordWrap => "wordWrap",
            EditorOption.StickyScroll => "stickyScroll",
            EditorOption.TabSize => "tabSize",
            EditorOption.InsertSpaces => "insertSpaces",
            EditorOption.DetectIndentation => "detectIndentation",
            EditorOption.AutoIndent => "autoIndent",
            EditorOption.EnableQuickSuggestions => "quickSuggestions",
            EditorOption.EnableWordBasedSuggestions => "wordBasedSuggestions",
            EditorOption.EnableSuggestOnTriggerCharacters => "suggestOnTriggerCharacters",
            EditorOption.EnableParameterHints => "parameterHints",
            _ => throw new ArgumentOutOfRangeException(nameof(option))
        };

        void UpdateOption(EditorOption option, object? value) {
            var monacoOption = ToMonacoOption(option);

            object? monacoValue = null;
            switch (option) {
                case EditorOption.LineNumbers:
                    if (value is not bool show)
                        throw new ArgumentException("LineNumbers requires boolean value.", nameof(value));
                    monacoValue = show ? "on" : "off";
                    break;
                case EditorOption.Minimap:
                    if (value is not bool enabled)
                        throw new ArgumentException("Minimap requires boolean value.", nameof(value));
                    monacoValue = new { enabled };
                    break;

                default:
                    monacoValue = value;
                    break;
            }

            SendCommand(EditorCommandType.UpdateOption, new {
                option = monacoOption,
                value = monacoValue
            });
        }

        public bool ShowLineNumbers {
            get { return (bool)GetValue(ShowLineNumbersProperty); }
            set { SetValue(ShowLineNumbersProperty, value); }
        }

        static void OnShowLineNumbersChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetShowLineNumbers((bool)e.NewValue);
        }

        void SetShowLineNumbers(bool show) {
            UpdateOption(EditorOption.LineNumbers, show);
        }


        public bool ShowMinimap {
            get { return (bool)GetValue(ShowMinimapProperty); }
            set { SetValue(ShowMinimapProperty, value); }
        }

        static void OnShowMinimapChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetShowMinimap((bool)e.NewValue);
        }

        void SetShowMinimap(bool show) {
            UpdateOption(EditorOption.Minimap, show);
        }


        public bool ShowGlyphMargin {
            get { return (bool)GetValue(ShowGlyphMarginProperty); }
            set { SetValue(ShowGlyphMarginProperty, value); }
        }

        static void OnShowGlyphMarginChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetShowGlyphMargin((bool)e.NewValue);
        }

        void SetShowGlyphMargin(bool show) {
            UpdateOption(EditorOption.GlyphMargin, show);
        }


        public bool EnableFolding {
            get { return (bool)GetValue(EnableFoldingProperty); }
            set { SetValue(EnableFoldingProperty, value); }
        }

        static void OnEnableFoldingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableFolding((bool)e.NewValue);
        }

        void SetEnableFolding(bool enabled) {
            UpdateOption(EditorOption.Folding, enabled);
        }


        public bool EnableContextMenu {
            get { return (bool)GetValue(EnableContextMenuProperty); }
            set { SetValue(EnableContextMenuProperty, value); }
        }

        static void OnEnableContextMenuChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableContextMenu((bool)e.NewValue);
        }

        public void SetEnableContextMenu(bool enabled) {
            UpdateOption(EditorOption.ContextMenu, enabled);
        }


        public bool EnableSmoothScrolling {
            get { return (bool)GetValue(EnableSmoothScrollingProperty); }
            set { SetValue(EnableSmoothScrollingProperty, value); }
        }

        static void OnEnableSmoothScrollingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableSmoothScrolling((bool)e.NewValue);
        }

        public void SetEnableSmoothScrolling(bool enabled) {
            UpdateOption(EditorOption.SmoothScrolling, enabled);
        }


        public bool EnableScrollBeyondLastLine {
            get { return (bool)GetValue(EnableScrollBeyondLastLineProperty); }
            set { SetValue(EnableScrollBeyondLastLineProperty, value); }
        }

        static void OnEnableScrollBeyondLastLineChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableScrollBeyondLastLine((bool)e.NewValue);
        }

        void SetEnableScrollBeyondLastLine(bool enabled) {
            UpdateOption(EditorOption.ScrollBeyondLastLine, enabled);
        }


        public int ScrollBeyondLastColumn {
            get { return (int)GetValue(ScrollBeyondLastColumnProperty); }
            set { SetValue(ScrollBeyondLastColumnProperty, value); }
        }

        static void ScrollBeyondLastColumnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetScrollBeyondLastColumn((int)e.NewValue);
        }

        public void SetScrollBeyondLastColumn(int columns) {
            UpdateOption(EditorOption.ScrollBeyondLastColumn, columns);
        }


        public int LineNumbersMinChars {
            get { return (int)GetValue(LineNumbersMinCharsProperty); }
            set { SetValue(LineNumbersMinCharsProperty, value); }
        }

        static void OnLineNumbersMinCharsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetLineNumbersMinChars((int)e.NewValue);
        }

        void SetLineNumbersMinChars(int minChars) {
            UpdateOption(EditorOption.LineNumbersMinChars, minChars);
        }


        public bool EnableDragAndDrop {
            get { return (bool)GetValue(EnableDragAndDropProperty); }
            set { SetValue(EnableDragAndDropProperty, value); }
        }

        static void OnEnableDragAndDropChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableDragAndDrop((bool)e.NewValue);
        }

        void SetEnableDragAndDrop(bool enabled) {
            UpdateOption(EditorOption.DragAndDrop, enabled);
        }


        public bool EnableMouseWheelZoom {
            get { return (bool)GetValue(EnableMouseWheelZoomProperty); }
            set { SetValue(EnableMouseWheelZoomProperty, value); }
        }

        static void EnableMouseWheelZoomChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableMouseWheelZoom((bool)e.NewValue);
        }

        void SetEnableMouseWheelZoom(bool enabled) {
            UpdateOption(EditorOption.MouseWheelZoom, enabled);
        }


        public EditorWordWrap WordWrap {
            get { return (EditorWordWrap)GetValue(WordWrapProperty); }
            set { SetValue(WordWrapProperty, value); }
        }

        static void WordWrapChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetWordWrap((EditorWordWrap)e.NewValue);
        }

        void SetWordWrap(EditorWordWrap wordWrap) {
            string monacoValue = wordWrap switch {
                EditorWordWrap.Off => "off",
                EditorWordWrap.On => "on",
                _ => throw new ArgumentOutOfRangeException(nameof(wordWrap))
            };
            UpdateOption(EditorOption.WordWrap, monacoValue);
        }


        public bool EnableStickyScroll{
            get { return (bool)GetValue(EnableStickyScrollProperty); }
            set { SetValue(EnableStickyScrollProperty, value); }
        }

        static void OnEnableStickyScrollChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableStickyScroll((bool)e.NewValue);
        }

        void SetEnableStickyScroll(bool enabled) {
            UpdateOption(EditorOption.StickyScroll, new { enabled = enabled });
        }


        //When DetectIndentation is enabled, Monaco may override TabSize based on the file content.
        public int TabSize {
            get { return (int)GetValue(TabSizeProperty); }
            set { SetValue(TabSizeProperty, value); }
        }

        static bool ValidateTabSize(object value) {
            if (value is int i)
                return i > 0 && i <= 64;

            return false;
        }

        static void OnTabSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetTabSize((int)e.NewValue);
        }

        public void SetTabSize(int size) {
            UpdateOption(EditorOption.TabSize, size);
        }


        public bool DetectIndentation {
            get { return (bool)GetValue(DetectIndentationProperty); }
            set { SetValue(DetectIndentationProperty, value); }
        }

        static void OnDetectIndentationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetDetectIndentation((bool)e.NewValue);
        }

        void SetDetectIndentation(bool detect) {
            UpdateOption(EditorOption.DetectIndentation, detect);
        }


        public bool InsertSpaces {
            get { return (bool)GetValue(InsertSpacesProperty); }
            set { SetValue(InsertSpacesProperty, value); }
        }

        static void OnInsertSpacesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetInsertSpaces((bool)e.NewValue);
        }

        void SetInsertSpaces(bool insertSpaces) {
            UpdateOption(EditorOption.InsertSpaces, insertSpaces);
        }


        // autoindent is not updated at runtime. you must to set some properties like TabSize to new value to force editor to use a new value.
        public EditorAutoIndent AutoIndent {
            get { return (EditorAutoIndent)GetValue(AutoIndentProperty); }
            set { SetValue(AutoIndentProperty, value); }
        }

        static void AutoIndentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetAutoIndent((EditorAutoIndent)e.NewValue);
        }

        void SetAutoIndent(EditorAutoIndent autoIndent) {
            string monacoValue = autoIndent switch {
                EditorAutoIndent.None => "none",
                EditorAutoIndent.Keep => "keep",
                EditorAutoIndent.Brackets => "brackets",
                EditorAutoIndent.Advanced => "advanced",
                EditorAutoIndent.Full => "full",
                _ => throw new ArgumentOutOfRangeException(nameof(autoIndent))
            };

            UpdateOption(EditorOption.AutoIndent, monacoValue);
            SetTabSize(TabSize); // a worcaround for Monaco resetting TabSize when AutoIndent is changed - we need to reapply it after changing AutoIndent.
        }


        public bool EnableQuickSuggestions {
            get { return (bool)GetValue(EnableQuickSuggestionsProperty); }
            set { SetValue(EnableQuickSuggestionsProperty, value); }
        }

        static void OnEnableQuickSuggestionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableQuickSuggestions((bool)e.NewValue);
        }

        void SetEnableQuickSuggestions(bool enabled) {
            UpdateOption(EditorOption.EnableQuickSuggestions, enabled);
        }


        public bool EnableWordBasedSuggestions {
            get { return (bool)GetValue(EnableWordBasedSuggestionsProperty); }
            set { SetValue(EnableWordBasedSuggestionsProperty, value); }
        }

        static void OnEnableWordBasedSuggestionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableWordBasedSuggestions((bool)e.NewValue);
        }
        void SetEnableWordBasedSuggestions(bool enabled) {
            var value = enabled ? "currentDocument" : "off";
            UpdateOption(EditorOption.EnableWordBasedSuggestions, value);
        }


        public bool EnableSuggestOnTriggerCharacters {
            get { return (bool)GetValue(EnableSuggestOnTriggerCharactersProperty); }
            set { SetValue(EnableSuggestOnTriggerCharactersProperty, value); }
        }
        static void OnEnableSuggestOnTriggerCharactersChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableSuggestOnTriggerCharacters((bool)e.NewValue);
        }

        void SetEnableSuggestOnTriggerCharacters(bool enabled) {
            UpdateOption(EditorOption.EnableSuggestOnTriggerCharacters, enabled);
        }


        public bool EnableParameterHints {
            get { return (bool)GetValue(EnableParameterHintsProperty); }
            set { SetValue(EnableParameterHintsProperty, value); }
        }

        static void OnEnableParameterHintsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableParameterHints((bool)e.NewValue);
        }

        void SetEnableParameterHints(bool enabled) {
            var value = new { enabled };
            UpdateOption(EditorOption.EnableParameterHints, value);
        }


        void SendCommand(EditorCommandType type, object? payload = null) {
            if (!editorReady)
                return;

            var cmd = new EditorCommand {
                Type = type,
                Payload = payload
            };

            var options = new JsonSerializerOptions(JsonSerializerOptions.Web);

            var json = JsonSerializer.Serialize(cmd, options);
            webView?.CoreWebView2.PostWebMessageAsJson(json);
        }


        public void RegisterTheme(MonacoTheme theme) {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            var payload = new {
                name = theme.Name,
                @base = MapBase(theme.Base),
                inherit = theme.Inherit,
                colors = theme.Colors?.ToDictionary(kvp => kvp.Key, kvp => ToHex(kvp.Value)) ?? new Dictionary<string, string>(),
                rules = (theme.Rules ?? Enumerable.Empty<MonacoThemeRule>())
                    .Select(ConvertRule)
                    .Where(r => r != null)
                    .ToList()
            };

            SendCommand(EditorCommandType.RegisterTheme, payload);
        }

        static Dictionary<string, object>? ConvertRule(MonacoThemeRule r) {
            var rule = new Dictionary<string, object> {
                ["token"] = r.Token
            };

            if (r.Foreground is Color fg)
                rule["foreground"] = ToHex(fg, false);

            if (r.Background is Color bg)
                rule["background"] = ToHex(bg, false);

            var fontStyle = ConvertFontStyle(r.FontStyle ?? MonacoFontStyle.None);
            if (!string.IsNullOrEmpty(fontStyle))
                rule["fontStyle"] = fontStyle;

            return rule.Count > 1 ? rule : null;
        }

        static string? ConvertFontStyle(MonacoFontStyle style) {
            if (style == MonacoFontStyle.None)
                return null;

            var sb = new StringBuilder(32); 

            if ((style & MonacoFontStyle.Bold) != 0)
                sb.Append("bold ");

            if ((style & MonacoFontStyle.Italic) != 0)
                sb.Append("italic ");

            if ((style & MonacoFontStyle.Underline) != 0)
                sb.Append("underline ");

            if (sb.Length == 0)
                return null;

            sb.Length--;
            return sb.ToString();
        }

        static string MapBase(MonacoThemeBase value) => value switch {
            MonacoThemeBase.Light => "vs",
            MonacoThemeBase.Dark => "vs-dark",
            MonacoThemeBase.HighContrast => "hc-black",
            _ => throw new ArgumentOutOfRangeException()
        };

        static string ToHex(Color c, bool addHashTag = true)
            => $"{(addHashTag ? "#" : string.Empty)}{c.R:X2}{c.G:X2}{c.B:X2}".ToLower();

        public string ThemeName {
            get { return (string)GetValue(ThemeNameProperty); }
            set { SetValue(ThemeNameProperty, value); }
        }

        static void OnThemeNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetTheme((string)e.NewValue);
        }

        void SetTheme(string themeName) {
            if (string.IsNullOrWhiteSpace(themeName))
                return;
            SendCommand(EditorCommandType.SetTheme, themeName);
        }


        void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e) {
            if (sender is not CoreWebView2)
                return;

            EditorMessage? message;

            try {
                message = JsonSerializer.Deserialize<EditorMessage>(e.WebMessageAsJson, JsonSerializerOptions.Web);
            } catch {
                return; 
            }

            if (message?.Type == null)
                return;

            switch (message.Type) {
                case EditorMessageType.TextChanged:
                    HandleTextChanged(message.Payload.GetString() ?? string.Empty);
                    break;
                case EditorMessageType.EditorReady:
                        HandleEditorReady();
                    break;
                case EditorMessageType.IsDirtyChanged:
                    IsModified = message.Payload.GetBoolean();
                    break;
                case EditorMessageType.Languages:
                    var langs = message.Payload
                        .EnumerateArray()
                        .Select(x => x.GetString()!)
                        .ToList();

                    LanguagesReceivedInternal?.Invoke(this, langs);
                    break;
                default:
                    break;
            }
        }

        void HandleTextChanged(string text) {

            updatingFromEditor = true;

            try {
                SetCurrentValue(TextProperty, text);
            } finally {
                updatingFromEditor = false;
            }
        }

        void HandleEditorReady() {
            if (editorReady)
                return;

            editorReady = true;
            ApplyCurrentState();
            RaiseEditorInitialized();
        }

        public event EventHandler? EditorInitialized;

        // This method raises the EditorInitialized event on the UI thread,
        // ensuring that any subscribers can safely interact with the editor control when they receive the event.
        void RaiseEditorInitialized() {
            if (Dispatcher.CheckAccess()) {
                EditorInitialized?.Invoke(this, EventArgs.Empty);
            } else {
                Dispatcher.Invoke(() =>
                    EditorInitialized?.Invoke(this, EventArgs.Empty));
            }
        }


        public string EditorLanguage {
            get { return (string)GetValue(EditorLanguageProperty); }
            set { SetValue(EditorLanguageProperty, value); }
        }

        static void OnEditorLanguageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            if (Equals(e.OldValue, e.NewValue))
                return;

            var control = (SyntaxEditor)sender;
            control.SetEditorLanguage((string)e.NewValue);
        }

        void SetEditorLanguage(string language) {
            if (string.IsNullOrWhiteSpace(language))
                return;

            this.SendCommand(EditorCommandType.SetLanguage, language);
        }

        public async Task<IReadOnlyList<string>> GetAvailableLanguagesAsync(CancellationToken cancellationToken = default) {
            
            var tcs = new TaskCompletionSource<IReadOnlyList<string>>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? s, IReadOnlyList<string> langs) {
                LanguagesReceivedInternal -= Handler;
                tcs.TrySetResult(langs);
            }

            LanguagesReceivedInternal += Handler;

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using (linkedCts.Token.Register(() => {
                LanguagesReceivedInternal -= Handler;
                tcs.TrySetCanceled(linkedCts.Token);
            })) {
                try {
                    SendCommand(EditorCommandType.GetLanguages);
                    return await tcs.Task.ConfigureAwait(false);
                } finally {
                    LanguagesReceivedInternal -= Handler;
                }
            }
        }

        // Language must contain Monarch and Configuration strings identical to how it is used in Monaco - JS object.
        public void RegisterLanguage(LanguageDescriptor language) {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            var payload = new {
                id = language.Id,
                monarch = language.Monarch,
                configuration = language.Configuration
            };
            SendCommand(EditorCommandType.RegisterLanguage, payload);
            registeredLanguages[language.Id] = language;
        }

        void RestoreRegisteredLanguages() {
            foreach (var language in registeredLanguages.Values) {
                RegisterLanguage(language);
            }
        }


        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            var newWebView = GetTemplateChild("PART_WebView") as WebView2;

            if (newWebView == null)
                throw new InvalidOperationException("PART_WebView not found.");

            if (webView == newWebView)
                return;

            DetachWebView();

            AttachWebView(newWebView);
        }

        void AttachWebView(WebView2 webView) {
            this.webView = webView;
            editorReady = false;
            _ = InitializeAsync();
        }

        async Task InitializeAsync() {
            var webView = this.webView;
            if (webView == null)
                return;

            await webView.EnsureCoreWebView2Async();

            if (this.webView != webView)
                return;

            webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            webView.CoreWebView2.ContextMenuRequested -= CoreWebView2_ContextMenuRequested;
            webView.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Monaco", "index.html");

            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            webView.Source = new Uri(path);
        }

        void CoreWebView2_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e) {
            e.Handled = true;
        }

        void DisposeWebView() {
            DetachWebView();
            webView?.Dispose();
            webView = null;
            editorReady = false;
        }

        public void Dispose() {
            if (disposed)
                return;

            DisposeWebView();
            disposed = true;
        }

        public void DetachWebView() {
            var webView = this.webView;
            if (webView?.CoreWebView2 != null) {
                webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                webView.CoreWebView2.ContextMenuRequested -= CoreWebView2_ContextMenuRequested;
            }
        }


        void ApplyCurrentState() {
            RestoreRegisteredLanguages();

            SetEditorLanguage(EditorLanguage);
            SetEditorReadOnly(ReadOnly);
            SetEditorText(Text);
            SetShowLineNumbers(ShowLineNumbers);
            SetShowMinimap(ShowMinimap);
            SetShowGlyphMargin(ShowGlyphMargin);
            SetEnableFolding(EnableFolding);
            SetEnableContextMenu(EnableContextMenu);
            SetEnableSmoothScrolling(EnableSmoothScrolling);
            SetEnableScrollBeyondLastLine(EnableScrollBeyondLastLine);
            SetScrollBeyondLastColumn(ScrollBeyondLastColumn);
            SetLineNumbersMinChars(LineNumbersMinChars);
            SetEnableDragAndDrop(EnableDragAndDrop);
            SetEnableMouseWheelZoom(EnableMouseWheelZoom);
            SetWordWrap(WordWrap);
            SetTheme(ThemeName);
            SetEnableStickyScroll(EnableStickyScroll);
            SetTabSize(TabSize);
            SetInsertSpaces(InsertSpaces);
            SetDetectIndentation(DetectIndentation);
            SetAutoIndent(AutoIndent);
            SetEnableQuickSuggestions(EnableQuickSuggestions);
            SetEnableWordBasedSuggestions(EnableWordBasedSuggestions);
            SetEnableSuggestOnTriggerCharacters(EnableSuggestOnTriggerCharacters);
            SetEnableParameterHints(EnableParameterHints);
        }
    }
}

