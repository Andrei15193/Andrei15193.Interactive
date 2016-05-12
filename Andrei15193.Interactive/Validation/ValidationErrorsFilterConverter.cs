using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml.Data;

namespace Andrei15193.Interactive.Validation
{
    public class ValidationErrorsFilterConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return null;

            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (!targetType.GetTypeInfo().IsAssignableFrom(typeof(IEnumerable<ValidationError>).GetTypeInfo()))
                throw new ArgumentException($"Result cannot be converted to {nameof(targetType)}.", nameof(targetType));
            var validationErrors = (IEnumerable<ValidationError>)value;

            if (parameter == null)
                return from validationError in validationErrors
                       where validationError?.MemberName == null
                       select validationError;

            var memberName = parameter as string;
            if (memberName != null)
                return from validationError in validationErrors
                       where StringComparer.OrdinalIgnoreCase.Equals(memberName, validationError?.MemberName)
                       select validationError;

            var memberNames = parameter as IEnumerable<string>;
            if (memberNames != null)
                return from validationError in validationErrors
                       where memberNames.Contains(validationError?.MemberName, StringComparer.OrdinalIgnoreCase)
                       select validationError;

            throw new ArgumentException($"The value provided to {nameof(parameter)} must be null, a string or a collection of strings.", nameof(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}