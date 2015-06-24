using System;
using System.Globalization;
using System.Windows.Data;

namespace SoftFluent.Windows
{
    public class PropertyGridConverter : IValueConverter
    {
        private static Type GetParameterAsType(object parameter)
        {
            if (parameter == null)
                return null;

            string typeName = string.Format("{0}", parameter);
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            return TypeResolutionService.ResolveType(typeName);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type parameterType = GetParameterAsType(parameter);
            if (parameterType != null)
            {
                value = ConversionService.ChangeType(value, parameterType, culture);
            }

            object convertedValue = targetType == null ? value : ConversionService.ChangeType(value, targetType, culture);
            return convertedValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object convertedValue = targetType == null ? value : ConversionService.ChangeType(value, targetType, culture);
            return convertedValue;
        }
    }
}