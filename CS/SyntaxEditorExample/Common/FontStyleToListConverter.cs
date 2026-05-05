using SyntaxEditor.Theming;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace SyntaxEditorExample.Common {
  

    internal class FontStyleToListConverter : MarkupExtension, IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return value;
            }

            var list = Enum.GetValues(typeof(MonacoFontStyle))
                .Cast<MonacoFontStyle>()
                .Where(c => ((MonacoFontStyle)value).HasFlag(c))
                .Cast<object>()
                .ToList();

            return list;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return value;
            }

            return ((List<object>)value).Cast<MonacoFontStyle>().Aggregate((x, y) => x |= y);
        }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            return this;
        }
    }
}
