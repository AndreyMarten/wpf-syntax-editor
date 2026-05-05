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
        static SyntaxEditor() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SyntaxEditor), new FrameworkPropertyMetadata(typeof(SyntaxEditor)));
        }

        public SyntaxEditor() {
            MarkAsSavedCommand = new DelegateCommand(MarkAsSaved);
        }

        private WebView2? _webView;
        private bool _editorReady;
        private bool _updatingFromEditor;

        #region Basic Properties and Commands

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SyntaxEditor), new FrameworkPropertyMetadata(string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        private static void OnTextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;

            if (control._updatingFromEditor)
                return;

            var text = e.NewValue as string ?? string.Empty;
            control.SetEditorText(text);
        }

        private void SetEditorText(string text) {
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

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(nameof(ReadOnly), typeof(bool), typeof(SyntaxEditor), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnReadOnlyChanged));

        private static void OnReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEditorReadOnly((bool)e.NewValue);
        }

        private void SetEditorReadOnly(bool readOnly) {
            this.SendCommand(EditorCommandType.SetReadOnly, readOnly);
        }

        public static readonly DependencyPropertyKey IsModifiedPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsModified),
                                                                             typeof(bool),
                                                                             typeof(SyntaxEditor),
                                                                             new PropertyMetadata(false));

        public static readonly DependencyProperty IsModifiedProperty = IsModifiedPropertyKey.DependencyProperty;

        public bool IsModified {
            get => (bool)GetValue(IsModifiedProperty);
            private set => SetValue(IsModifiedPropertyKey, value);
        }

        #endregion Basic Properties and Commands

        #region Options

        private static string ToMonacoOption(EditorOption option) => option switch {
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

        private void UpdateOption(EditorOption option, object? value) {
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

        #region ShowLineNumbers
        public bool ShowLineNumbers {
            get { return (bool)GetValue(ShowLineNumbersProperty); }
            set { SetValue(ShowLineNumbersProperty, value); }
        }

        public static readonly DependencyProperty ShowLineNumbersProperty =
            DependencyProperty.Register(nameof(ShowLineNumbers), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnShowLineNumbersChanged));

        private static void OnShowLineNumbersChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetShowLineNumbers((bool)e.NewValue);
        }

        private void SetShowLineNumbers(bool show) {
            UpdateOption(EditorOption.LineNumbers, show);
        }

        #endregion ShowLineNumbers

        #region ShowMinimap

        public bool ShowMinimap {
            get { return (bool)GetValue(ShowMinimapProperty); }
            set { SetValue(ShowMinimapProperty, value); }
        }

        public static readonly DependencyProperty ShowMinimapProperty =
            DependencyProperty.Register(nameof(ShowMinimap), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(false, OnShowMinimapChanged));

        private static void OnShowMinimapChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetShowMinimap((bool)e.NewValue);
        }

        private void SetShowMinimap(bool show) {
            UpdateOption(EditorOption.Minimap, show);
        }

        #endregion ShowMinimap

        #region ShowGlyphMargin

        public bool ShowGlyphMargin {
            get { return (bool)GetValue(ShowGlyphMarginProperty); }
            set { SetValue(ShowGlyphMarginProperty, value); }
        }

        public static readonly DependencyProperty ShowGlyphMarginProperty =
            DependencyProperty.Register(nameof(ShowGlyphMargin), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(false, OnShowGlyphMarginChanged));

        private static void OnShowGlyphMarginChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetShowGlyphMargin((bool)e.NewValue);
        }

        private void SetShowGlyphMargin(bool show) {
            UpdateOption(EditorOption.GlyphMargin, show);
        }

        #endregion ShowGlyphMargin

        #region EnableFolding

        public bool EnableFolding {
            get { return (bool)GetValue(EnableFoldingProperty); }
            set { SetValue(EnableFoldingProperty, value); }
        }

        public static readonly DependencyProperty EnableFoldingProperty =
            DependencyProperty.Register(nameof(EnableFolding), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableFoldingChanged));

        private static void OnEnableFoldingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableFolding((bool)e.NewValue);
        }

        private void SetEnableFolding(bool enabled) {
            UpdateOption(EditorOption.Folding, enabled);
        }

        #endregion EnableFolding

        #region EnableContextMenu

        public bool EnableContextMenu {
            get { return (bool)GetValue(EnableContextMenuProperty); }
            set { SetValue(EnableContextMenuProperty, value); }
        }

        public static readonly DependencyProperty EnableContextMenuProperty =
            DependencyProperty.Register(nameof(EnableContextMenu), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableContextMenuChanged));

        private static void OnEnableContextMenuChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableContextMenu((bool)e.NewValue);
        }

        public void SetEnableContextMenu(bool enabled) {
            UpdateOption(EditorOption.ContextMenu, enabled);
        }

        #endregion EnableContextMenu

        #region EnableSmoothScrolling

        public bool EnableSmoothScrolling {
            get { return (bool)GetValue(EnableSmoothScrollingProperty); }
            set { SetValue(EnableSmoothScrollingProperty, value); }
        }

        public static readonly DependencyProperty EnableSmoothScrollingProperty =
            DependencyProperty.Register(nameof(EnableSmoothScrolling), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(false, OnEnableSmoothScrollingChanged));

        private static void OnEnableSmoothScrollingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableSmoothScrolling((bool)e.NewValue);
        }

        public void SetEnableSmoothScrolling(bool enabled) {
            UpdateOption(EditorOption.SmoothScrolling, enabled);
        }

        #endregion EnableSmoothScrolling

        #region EnableScrollBeyondLastLine

        public bool EnableScrollBeyondLastLine {
            get { return (bool)GetValue(EnableScrollBeyondLastLineProperty); }
            set { SetValue(EnableScrollBeyondLastLineProperty, value); }
        }

        public static readonly DependencyProperty EnableScrollBeyondLastLineProperty =
            DependencyProperty.Register(nameof(EnableScrollBeyondLastLine), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableScrollBeyondLastLineChanged));

        private static void OnEnableScrollBeyondLastLineChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableScrollBeyondLastLine((bool)e.NewValue);
        }

        private void SetEnableScrollBeyondLastLine(bool enabled) {
            UpdateOption(EditorOption.ScrollBeyondLastLine, enabled);
        }

        #endregion EnableScrollBeyondLastLine

        #region ScrollBeyondLastColumn

        public int ScrollBeyondLastColumn {
            get { return (int)GetValue(ScrollBeyondLastColumnProperty); }
            set { SetValue(ScrollBeyondLastColumnProperty, value); }
        }

        public static readonly DependencyProperty ScrollBeyondLastColumnProperty =
            DependencyProperty.Register(nameof(ScrollBeyondLastColumn), typeof(int), typeof(SyntaxEditor), new PropertyMetadata(5, ScrollBeyondLastColumnChanged));

        private static void ScrollBeyondLastColumnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetScrollBeyondLastColumn((int)e.NewValue);
        }

        public void SetScrollBeyondLastColumn(int columns) {
            UpdateOption(EditorOption.ScrollBeyondLastColumn, columns);
        }

        #endregion ScrollBeyondLastColumn

        #region LineNumbersMinChars

        public int LineNumbersMinChars {
            get { return (int)GetValue(LineNumbersMinCharsProperty); }
            set { SetValue(LineNumbersMinCharsProperty, value); }
        }

        public static readonly DependencyProperty LineNumbersMinCharsProperty =
            DependencyProperty.Register(nameof(LineNumbersMinChars), typeof(int), typeof(SyntaxEditor), new PropertyMetadata(5, OnLineNumbersMinCharsChanged));

        private static void OnLineNumbersMinCharsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetLineNumbersMinChars((int)e.NewValue);
        }

        private void SetLineNumbersMinChars(int minChars) {
            UpdateOption(EditorOption.LineNumbersMinChars, minChars);
        }

        #endregion LineNumbersMinChars

        #region EnableDragAndDrop

        public bool EnableDragAndDrop {
            get { return (bool)GetValue(EnableDragAndDropProperty); }
            set { SetValue(EnableDragAndDropProperty, value); }
        }

        public static readonly DependencyProperty EnableDragAndDropProperty =
            DependencyProperty.Register(nameof(EnableDragAndDrop), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableDragAndDropChanged));

        private static void OnEnableDragAndDropChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableDragAndDrop((bool)e.NewValue);
        }

        private void SetEnableDragAndDrop(bool enabled) {
            UpdateOption(EditorOption.DragAndDrop, enabled);
        }

        #endregion EnableDragAndDrop

        #region EnableMouseWheelZoom

        public bool EnableMouseWheelZoom {
            get { return (bool)GetValue(EnableMouseWheelZoomProperty); }
            set { SetValue(EnableMouseWheelZoomProperty, value); }
        }

        public static readonly DependencyProperty EnableMouseWheelZoomProperty =
            DependencyProperty.Register(nameof(EnableMouseWheelZoom), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(false, EnableMouseWheelZoomChanged));

        private static void EnableMouseWheelZoomChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableMouseWheelZoom((bool)e.NewValue);
        }

        private void SetEnableMouseWheelZoom(bool enabled) {
            UpdateOption(EditorOption.MouseWheelZoom, enabled);
        }

        #endregion EnableMouseWheelZoom

        #region WordWrap

        public EditorWordWrap WordWrap {
            get { return (EditorWordWrap)GetValue(WordWrapProperty); }
            set { SetValue(WordWrapProperty, value); }
        }

        public static readonly DependencyProperty WordWrapProperty =
            DependencyProperty.Register(nameof(WordWrap), typeof(EditorWordWrap), typeof(SyntaxEditor), new PropertyMetadata(EditorWordWrap.Off, WordWrapChanged));

        private static void WordWrapChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetWordWrap((EditorWordWrap)e.NewValue);
        }

        private void SetWordWrap(EditorWordWrap wordWrap) {
            string monacoValue = wordWrap switch {
                EditorWordWrap.Off => "off",
                EditorWordWrap.On => "on",
                _ => throw new ArgumentOutOfRangeException(nameof(wordWrap))
            };
            UpdateOption(EditorOption.WordWrap, monacoValue);
        }

        #endregion WordWrap 

        #region EnableStickyScroll

        public bool EnableStickyScroll{
            get { return (bool)GetValue(EnableStickyScrollProperty); }
            set { SetValue(EnableStickyScrollProperty, value); }
        }

        public static readonly DependencyProperty EnableStickyScrollProperty =
            DependencyProperty.Register(nameof(EnableStickyScroll), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableStickyScrollChanged));

        private static void OnEnableStickyScrollChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableStickyScroll((bool)e.NewValue);
        }

        private void SetEnableStickyScroll(bool enabled) {
            UpdateOption(EditorOption.StickyScroll, new { enabled = enabled });
        }

        #endregion EnableStickyScroll

        #region TabSize

        //When DetectIndentation is enabled, Monaco may override TabSize based on the file content.
        public int TabSize {
            get { return (int)GetValue(TabSizeProperty); }
            set { SetValue(TabSizeProperty, value); }
        }

        public static readonly DependencyProperty TabSizeProperty =
            DependencyProperty.Register(nameof(TabSize), typeof(int), typeof(SyntaxEditor), new PropertyMetadata(4, OnTabSizeChanged), ValidateTabSize);

        private static bool ValidateTabSize(object value) {
            if (value is int i)
                return i > 0 && i <= 64;

            return false;
        }

        private static void OnTabSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetTabSize((int)e.NewValue);
        }

        public void SetTabSize(int size) {
            UpdateOption(EditorOption.TabSize, size);
        }

        #endregion TabSize

        #region DetectIndentation

        public bool DetectIndentation {
            get { return (bool)GetValue(DetectIndentationProperty); }
            set { SetValue(DetectIndentationProperty, value); }
        }

        public static readonly DependencyProperty DetectIndentationProperty =
            DependencyProperty.Register(nameof(DetectIndentation), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnDetectIndentationChanged));

        private static void OnDetectIndentationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetDetectIndentation((bool)e.NewValue);
        }

        private void SetDetectIndentation(bool detect) {
            UpdateOption(EditorOption.DetectIndentation, detect);
        }

        #endregion DetectIndentation

        #region InsertSpaces

        public bool InsertSpaces {
            get { return (bool)GetValue(InsertSpacesProperty); }
            set { SetValue(InsertSpacesProperty, value); }
        }

        public static readonly DependencyProperty InsertSpacesProperty =
            DependencyProperty.Register(nameof(InsertSpaces), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnInsertSpacesChanged));

        private static void OnInsertSpacesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetInsertSpaces((bool)e.NewValue);
        }

        private void SetInsertSpaces(bool insertSpaces) {
            UpdateOption(EditorOption.InsertSpaces, insertSpaces);
        }

        #endregion InsertSpaces

        #region AutoIndent

        // autoindent is not updated at runtime. you must to set some properties like TabSize to new value to force editor to use a new value.
        public EditorAutoIndent AutoIndent {
            get { return (EditorAutoIndent)GetValue(AutoIndentProperty); }
            set { SetValue(AutoIndentProperty, value); }
        }

        public static readonly DependencyProperty AutoIndentProperty =
            DependencyProperty.Register(nameof(AutoIndent), typeof(EditorAutoIndent), typeof(SyntaxEditor), new PropertyMetadata(EditorAutoIndent.Full, AutoIndentChanged));

        private static void AutoIndentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetAutoIndent((EditorAutoIndent)e.NewValue);
        }

        private void SetAutoIndent(EditorAutoIndent autoIndent) {
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

        #endregion AutoIndent

        #region EnableQuickSuggestions

        public bool EnableQuickSuggestions {
            get { return (bool)GetValue(EnableQuickSuggestionsProperty); }
            set { SetValue(EnableQuickSuggestionsProperty, value); }
        }

        public static readonly DependencyProperty EnableQuickSuggestionsProperty =
            DependencyProperty.Register(nameof(EnableQuickSuggestions), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableQuickSuggestionsChanged));

        private static void OnEnableQuickSuggestionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableQuickSuggestions((bool)e.NewValue);
        }

        private void SetEnableQuickSuggestions(bool enabled) {
            UpdateOption(EditorOption.EnableQuickSuggestions, enabled);
        }

        #endregion EnableQuickSuggestions

        #region EnableWordBasedSuggestions

        public bool EnableWordBasedSuggestions {
            get { return (bool)GetValue(EnableWordBasedSuggestionsProperty); }
            set { SetValue(EnableWordBasedSuggestionsProperty, value); }
        }

        public static readonly DependencyProperty EnableWordBasedSuggestionsProperty =
            DependencyProperty.Register(nameof(EnableWordBasedSuggestions), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableWordBasedSuggestionsChanged));

        private static void OnEnableWordBasedSuggestionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableWordBasedSuggestions((bool)e.NewValue);
        }
        private void SetEnableWordBasedSuggestions(bool enabled) {
            var value = enabled ? "currentDocument" : "off";
            UpdateOption(EditorOption.EnableWordBasedSuggestions, value);
        }

        #endregion EnableWordBasedSuggestions

        #region EnableSuggestOnTriggerCharacters

        public bool EnableSuggestOnTriggerCharacters {
            get { return (bool)GetValue(EnableSuggestOnTriggerCharactersProperty); }
            set { SetValue(EnableSuggestOnTriggerCharactersProperty, value); }
        }
        public static readonly DependencyProperty EnableSuggestOnTriggerCharactersProperty =
            DependencyProperty.Register(nameof(EnableSuggestOnTriggerCharacters), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableSuggestOnTriggerCharactersChanged));

        private static void OnEnableSuggestOnTriggerCharactersChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableSuggestOnTriggerCharacters((bool)e.NewValue);
        }

        private void SetEnableSuggestOnTriggerCharacters(bool enabled) {
            UpdateOption(EditorOption.EnableSuggestOnTriggerCharacters, enabled);
        }

        #endregion EnableSuggestOnTriggerCharacters

        #region EnableParameterHints

        public bool EnableParameterHints {
            get { return (bool)GetValue(EnableParameterHintsProperty); }
            set { SetValue(EnableParameterHintsProperty, value); }
        }

        public static readonly DependencyProperty EnableParameterHintsProperty =
            DependencyProperty.Register(nameof(EnableParameterHints), typeof(bool), typeof(SyntaxEditor), new PropertyMetadata(true, OnEnableParameterHintsChanged));

        private static void OnEnableParameterHintsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetEnableParameterHints((bool)e.NewValue);
        }

        private void SetEnableParameterHints(bool enabled) {
            var value = new { enabled };
            UpdateOption(EditorOption.EnableParameterHints, value);
        }

        #endregion EnableParameterHints

        #endregion Options

        private void SendCommand(EditorCommandType type, object? payload = null) {
            if (!_editorReady)
                return;

            var cmd = new EditorCommand {
                Type = type,
                Payload = payload
            };

            var options = new JsonSerializerOptions(JsonSerializerOptions.Web);

            var json = JsonSerializer.Serialize(cmd, options);
            _webView?.CoreWebView2.PostWebMessageAsJson(json);
        }

        #region Theming

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

        private static Dictionary<string, object>? ConvertRule(MonacoThemeRule r) {
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

        private static string? ConvertFontStyle(MonacoFontStyle style) {
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

        private static string MapBase(MonacoThemeBase value) => value switch {
            MonacoThemeBase.Light => "vs",
            MonacoThemeBase.Dark => "vs-dark",
            MonacoThemeBase.HighContrast => "hc-black",
            _ => throw new ArgumentOutOfRangeException()
        };

        private static string ToHex(Color c, bool addHashTag = true)
            => $"{(addHashTag ? "#" : string.Empty)}{c.R:X2}{c.G:X2}{c.B:X2}".ToLower();

        public string ThemeName {
            get { return (string)GetValue(ThemeNameProperty); }
            set { SetValue(ThemeNameProperty, value); }
        }

        public static readonly DependencyProperty ThemeNameProperty =
            DependencyProperty.Register(nameof(ThemeName), typeof(string), typeof(SyntaxEditor), new PropertyMetadata("vs", OnThemeNameChanged));

        private static void OnThemeNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var control = (SyntaxEditor)sender;
            control.SetTheme((string)e.NewValue);
        }

        private void SetTheme(string themeName) {
            if (string.IsNullOrWhiteSpace(themeName))
                return;
            SendCommand(EditorCommandType.SetTheme, themeName);
        }

        #endregion Theming

        #region Processing Monaco Messages

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e) {
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

        private void HandleTextChanged(string text) {

            _updatingFromEditor = true;

            try {
                SetCurrentValue(TextProperty, text);
            } finally {
                _updatingFromEditor = false;
            }
        }

        private void HandleEditorReady() {
            if (_editorReady)
                return;

            _editorReady = true;
            ApplyCurrentState();
            RaiseEditorInitialized();
        }

        public event EventHandler? EditorInitialized;

        // This method raises the EditorInitialized event on the UI thread,
        // ensuring that any subscribers can safely interact with the editor control when they receive the event.
        private void RaiseEditorInitialized() {
            if (Dispatcher.CheckAccess()) {
                EditorInitialized?.Invoke(this, EventArgs.Empty);
            } else {
                Dispatcher.Invoke(() =>
                    EditorInitialized?.Invoke(this, EventArgs.Empty));
            }
        }

        #endregion Processing Monaco Messages

        #region Language Support

        public string EditorLanguage {
            get { return (string)GetValue(EditorLanguageProperty); }
            set { SetValue(EditorLanguageProperty, value); }
        }

        public static readonly DependencyProperty EditorLanguageProperty =
            DependencyProperty.Register(nameof(EditorLanguage), typeof(string), typeof(SyntaxEditor), new FrameworkPropertyMetadata("csharp", OnEditorLanguageChanged));

        private static void OnEditorLanguageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            if (Equals(e.OldValue, e.NewValue))
                return;

            var control = (SyntaxEditor)sender;
            control.SetEditorLanguage((string)e.NewValue);
        }

        private void SetEditorLanguage(string language) {
            if (string.IsNullOrWhiteSpace(language))
                return;

            this.SendCommand(EditorCommandType.SetLanguage, language);
        }

        private event EventHandler<IReadOnlyList<string>>? LanguagesReceivedInternal;

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

        // Cache for registered languages, to restore when webview2 is recreated.
        // Note: This is a simple cache and does not handle updates to existing languages or removal of languages.
        private readonly Dictionary<string, LanguageDescriptor> _registeredLanguages = new();

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
            _registeredLanguages[language.Id] = language;
        }

        private void RestoreRegisteredLanguages() {
            foreach (var language in _registeredLanguages.Values) {
                RegisterLanguage(language);
            }
        }

        #endregion Language Support

        #region Initialization and Cleanup

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            var newWebView = GetTemplateChild("PART_WebView") as WebView2;

            if (newWebView == null)
                throw new InvalidOperationException("PART_WebView not found.");

            if (_webView == newWebView)
                return;

            DetachWebView();

            AttachWebView(newWebView);
        }

        private void AttachWebView(WebView2 webView) {
            _webView = webView;
            _editorReady = false;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync() {
            var webView = _webView;
            if (webView == null)
                return;

            await webView.EnsureCoreWebView2Async();

            if (_webView != webView)
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

        private void CoreWebView2_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e) {
            e.Handled = true;
        }

        private void DisposeWebView() {
            DetachWebView();
            _webView?.Dispose();
            _webView = null;
            _editorReady = false;
        }

        private bool _disposed;

        public void Dispose() {
            if (_disposed)
                return;

            DisposeWebView();
            _disposed = true;
        }

        public void DetachWebView() {
            var webView = _webView;
            if (webView?.CoreWebView2 != null) {
                webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                webView.CoreWebView2.ContextMenuRequested -= CoreWebView2_ContextMenuRequested;
            }
        }

        #endregion Initialization and Cleanup

        private void ApplyCurrentState() {
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
