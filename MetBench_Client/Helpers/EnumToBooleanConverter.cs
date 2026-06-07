using System;
using System.Globalization;
using System.Windows.Data;

namespace MetBench_Client.Helpers
{
    internal class EnumToBooleanConverter : IValueConverter
    {
        public EnumToBooleanConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string enumString)
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
            }

            if (value is not Enum enumValue || !Enum.IsDefined(value.GetType(), value))
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
            }

            var expectedValue = Enum.Parse(value.GetType(), enumString);
            return expectedValue.Equals(enumValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string enumString)
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
            }

            var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (!enumType.IsEnum)
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterTargetTypeMustBeAnEnum");
            }

            return Enum.Parse(enumType, enumString);
        }
    }
}
