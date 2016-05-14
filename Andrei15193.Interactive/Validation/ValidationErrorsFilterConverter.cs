using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

            var filteredValidationErrors = validationErrors.Where(GetValidationErrorFilterFor(parameter));
            var collectionChangedNotifier = validationErrors as INotifyCollectionChanged;
            if (collectionChangedNotifier != null)
            {
                var observableFilteredValidationErrors = new ObservableCollection<ValidationError>(filteredValidationErrors);
                collectionChangedNotifier.CollectionChanged +=
                    (sender, e) =>
                    {
                        observableFilteredValidationErrors.Clear();
                        foreach (var validationError in filteredValidationErrors)
                            observableFilteredValidationErrors.Add(validationError);
                    };
                return new ReadOnlyObservableCollection<ValidationError>(observableFilteredValidationErrors);
            }
            else
                return filteredValidationErrors.ToList();
        }

        private static Func<ValidationError, bool> GetValidationErrorFilterFor(object parameter)
        {
            if (parameter == null)
                return validationError => validationError?.MemberName == null;

            var memberName = parameter as string;
            if (memberName != null)
                return validationError => StringComparer.OrdinalIgnoreCase.Equals(memberName, validationError?.MemberName);

            var memberNames = parameter as IEnumerable<string>;
            if (memberNames != null)
                return validationError => memberNames.Contains(validationError?.MemberName, StringComparer.OrdinalIgnoreCase);

            throw new ArgumentException($"The value provided to {nameof(parameter)} must be null, a string or a collection of strings.", nameof(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}