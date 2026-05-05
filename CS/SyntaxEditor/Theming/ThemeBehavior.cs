using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SyntaxEditor.Theming {
    public class ThemeBehavior : Behavior<SyntaxEditor> {

        static ThemeBehavior() {
            if (!CompatibilitySettings.UseLightweightThemes) {
                throw new InvalidOperationException("Lightweight themes must be used to use MonacoThemeBehavior.");
            }
        }

        protected override void OnAttached() {
            base.OnAttached();
            LightweightThemeManager.CurrentThemeChanged += LightweightThemeManager_CurrentThemeChanged;
            this.AssociatedObject.EditorInitialized += AssociatedObject_EditorInitialized;
        }


        public IReadOnlyList<MonacoThemeRule>? Rules {
            get => (IReadOnlyList<MonacoThemeRule>?)GetValue(RulesProperty);
            set => SetValue(RulesProperty, value);
        }

        public static readonly DependencyProperty RulesProperty = 
            DependencyProperty.Register(nameof(Rules), typeof(IReadOnlyList<MonacoThemeRule>), typeof(ThemeBehavior), new PropertyMetadata(null, OnRulesChanged));

        private static void OnRulesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var behavior = (ThemeBehavior)sender;
            behavior.ApplyCurrentTheme();
        }

        public bool ApplyDevExpressColors {
            get { return (bool)GetValue(ApplyDevExpressColorsProperty); }
            set { SetValue(ApplyDevExpressColorsProperty, value); }
        }

        public static readonly DependencyProperty ApplyDevExpressColorsProperty =
            DependencyProperty.Register(nameof(ApplyDevExpressColors), typeof(bool), typeof(ThemeBehavior), new PropertyMetadata(true, OnApplyDevExpressColorsChanged));

        private static void OnApplyDevExpressColorsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            var behavior = (ThemeBehavior)sender;
            behavior.ApplyCurrentTheme();
        }

        private void LightweightThemeManager_CurrentThemeChanged(object sender, ValueChangedEventArgs<LightweightTheme> e) {
            ApplyCurrentTheme();
        }

        private void AssociatedObject_EditorInitialized(object? sender, EventArgs e) {
            ApplyCurrentTheme();
        }

        private void AdjustDXColors(MonacoTheme monacoTheme, string dxTheme) {
            if(monacoTheme.Colors == null)
                return;

            bool useSystemColors = ApplicationThemeHelper.ApplicationThemeName.Contains("SystemColors");

            switch (dxTheme) {
                case "Win11Light":
                    monacoTheme.Colors[MonacoColorKeys.SelectionBackground] = monacoTheme.Colors[MonacoColorKeys.SelectionBackground].Lighten(0.5);
                    monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground] = monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground].Lighten(0.5);
                    break;
                case "VS2019Light":
                    monacoTheme.Colors[MonacoColorKeys.SelectionBackground] = monacoTheme.Colors[MonacoColorKeys.SelectionBackground].Lighten(0.5);
                    monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground] = monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground].Lighten(0.5);
                    break;
                case "Office2019Colorful":
                case "Win10Light":
                case "VS2019Blue":
                    monacoTheme.Colors[MonacoColorKeys.SuggestWidgetSelectedBackground] = monacoTheme.Colors[MonacoColorKeys.SuggestWidgetSelectedBackground].Darken(0.3);
                    break;
                case "Win11Dark":
                    monacoTheme.Colors[MonacoColorKeys.SelectionBackground] = monacoTheme.Colors[MonacoColorKeys.SelectionBackground].Darken(useSystemColors ? 0.2 : 0.4);
                    monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground] = monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground].Darken(useSystemColors ? 0.2 : 0.4);
                    monacoTheme.Colors[MonacoColorKeys.SuggestWidgetSelectedBackground] = monacoTheme.Colors[MonacoColorKeys.SuggestWidgetSelectedBackground].Darken(useSystemColors ? 0.2 : 0.4);
                    break;
                case "Win10Dark":
                    monacoTheme.Colors[MonacoColorKeys.SelectionBackground] = monacoTheme.Colors[MonacoColorKeys.SelectionBackground].Darken(0.2);
                    monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground] = monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground].Darken(0.2);
                    break;
                case "VS2019Dark":
                    monacoTheme.Colors[MonacoColorKeys.SelectionBackground] = monacoTheme.Colors[MonacoColorKeys.SelectionBackground].Darken(0.1);
                    monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground] = monacoTheme.Colors[MonacoColorKeys.InactiveSelectionBackground].Darken(0.1);
                    break;
                default:
                    break;
            }
        }

        private void ApplyCurrentTheme() {
            var dxTheme = LightweightThemeManager.CurrentTheme;
            
            var monacoTheme = CreateFromDXTheme(dxTheme.Name, this.Rules, this.ApplyDevExpressColors);
            AdjustDXColors(monacoTheme, dxTheme.Name);

            AssociatedObject.RegisterTheme(monacoTheme);
            AssociatedObject.ThemeName = monacoTheme.Name;
        }

        public static Color? GetColor(string colorKey) {
            var palette = LightweightThemeManager.CurrentTheme.Palette;
            var key = $"Color.{colorKey}";

            if (palette.Contains(key) && palette[key] is Color color) {
                return color;
            }

            return null;
        }

        public static MonacoTheme CreateFromDXTheme(string DXThemeName, IReadOnlyList<MonacoThemeRule>? rules = null, bool applyDevExpressColors = false) {
            var baseKind = ResolveBase(DXThemeName);

            var result = new MonacoTheme {
                Name = $"{DXThemeName.ToLowerInvariant()}",
                Base = baseKind,
                Colors = applyDevExpressColors ? CreateMonacoColors() : null,
                Rules = rules
            };

            return result;
        }

        private static Dictionary<string, Color> CreateMonacoColors() {
            var result = new Dictionary<string, Color>();

            Color? Try(params string[] keys) {
                foreach (var key in keys) {
                    var color = GetColor(key);
                    if (color.HasValue)
                        return color;
                }
                
                return null;
            }

            void Map(string monacoKey, params string[] dxKeys) {
                var color = Try(dxKeys);
                if (color.HasValue)
                    result[monacoKey] = color.Value;
            }

            // === Base ===
            Map(MonacoColorKeys.EditorBackground,
                "Editor.Background",
                "Control.Background",
                "WindowBackground");

            Map(MonacoColorKeys.EditorForeground,
                "Foreground.Primary",
                "Foreground",
                "Editor.Foreground");

            // === Line numbers ===
            Map(MonacoColorKeys.LineNumberForeground,
                "Foreground.Secondary",
                "Foreground.Disabled",
                "Foreground");

            Map(MonacoColorKeys.LineNumberActiveForeground,
                "Foreground.Primary",
                "Foreground");

            // === Cursor & selection ===
            Map(MonacoColorKeys.CursorForeground,
                "Accent",
                "Foreground.Primary",
                "Foreground");

            Map(MonacoColorKeys.SelectionBackground,
                "Accent",
                "SelectionBackground",
                "Selection");
                

            Map(MonacoColorKeys.InactiveSelectionBackground,
                "SelectionBackground",
                "Selection",
                "Accent");

            // === Current line ===
            Map(MonacoColorKeys.LineHighlightBackground,
                "Control.Background",
                "Editor.Background");

            // === Gutter ===
            Map(MonacoColorKeys.GutterBackground,
                "Editor.Background",
                "Control.Background");

            // === Indent guides ===
            Map(MonacoColorKeys.IndentGuideBackground,
                "Delimiter",
                "Border");

            Map(MonacoColorKeys.IndentGuideActiveBackground,
                "Foreground.Secondary",
                "Foreground");

            // === Scrollbar ===
            Map(MonacoColorKeys.ScrollbarShadow,
                "Delimiter",
                "Control.Background");

            Map(MonacoColorKeys.ScrollbarSliderBackground,
                "Border",
                "Button.Background");

            Map(MonacoColorKeys.ScrollbarSliderHoverBackground,
                "Delimiter",
                "Backstage.Delimiter",
                "Control.Background");

            Map(MonacoColorKeys.ScrollbarSliderActiveBackground,
                "Border",
                "Control.Background");

            // === Brackets ===
            Map(MonacoColorKeys.BracketMatchBackground,
                "Control.Background",
                "Editor.Background");

            Map(MonacoColorKeys.BracketMatchBorder,
                "Accent",
                "Border");

            // === Find ===
            Map(MonacoColorKeys.FindMatchBackground,
                "Accent",
                "SelectionBackground",
                "Selection");

            Map(MonacoColorKeys.FindMatchHighlightBackground,
                "SelectionBackground",
                "Selection",
                "Accent");

            // === Hover ===
            Map(MonacoColorKeys.HoverWidgetBackground,
                "Delimiter",
                "Backstage.Delimiter",
                "Control.Background");

            Map(MonacoColorKeys.HoverWidgetBorder,
                "Editor.Border",
                "Border");

            // === Suggest ===
            Map(MonacoColorKeys.SuggestWidgetBackground,
                "FlyoutBackground",
                "PanelBackground",
                "Control.Background");

            Map(MonacoColorKeys.SuggestWidgetSelectedBackground,
                "SelectionBackground",
                "Selection",
                "Accent");

            return result;
        }

        private static MonacoThemeBase ResolveBase(string themeName) {
            if (string.IsNullOrWhiteSpace(themeName))
                return MonacoThemeBase.Light;

            if (themeName.Contains("HighContrast", StringComparison.OrdinalIgnoreCase))
                return MonacoThemeBase.HighContrast;

            if (themeName.Contains("Dark", StringComparison.OrdinalIgnoreCase) ||
                themeName.Contains("Black", StringComparison.OrdinalIgnoreCase))
                return MonacoThemeBase.Dark;

            return MonacoThemeBase.Light;
        }

        protected override void OnDetaching() {
            if (AssociatedObject != null) {
                this.AssociatedObject.EditorInitialized -= AssociatedObject_EditorInitialized;
            }
            LightweightThemeManager.CurrentThemeChanged -= LightweightThemeManager_CurrentThemeChanged;

            base.OnDetaching();
        }
    }

   
}
