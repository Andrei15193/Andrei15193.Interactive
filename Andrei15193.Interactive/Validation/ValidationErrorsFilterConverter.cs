using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml.Data;

namespace Andrei15193.Interactive.Validation
{
    /// <summary>
    /// A one way converter to filter <see cref="ValidationError"/>s for a specific
    /// member or at the entity level.
    /// </summary>
    public class ValidationErrorsFilterConverter
        : IValueConverter
    {
        /// <summary>
        /// Converts the provided <paramref name="value"/> which must implement
        /// <see cref="IEnumerable{ValidationError}"/>.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <param name="targetType">
        /// The type to convert to which must be <see cref="IEnumerable{ValidationError}"/>.
        /// </param>
        /// <param name="parameter">
        /// The parameter which can be null to filter for entity level errors, a string
        /// containg the member name to filter <see cref="ValidationError"/>s for, or an
        /// <see cref="IEnumerable{String}"/> which contains all the member names
        /// to filter <see cref="ValidationError"/>s for.
        /// </param>
        /// <param name="language">
        /// The parameter is ignored, string comparison is done using the
        /// <see cref="StringComparer.OrdinalIgnoreCase"/> comparer.
        /// </param>
        /// <returns>
        /// Returns a collection of filtered <see cref="ValidationError"/>s based on the
        /// value provided through <paramref name="parameter"/>. If the provided
        /// <paramref name="value"/> implements <see cref="INotifyCollectionChanged"/>
        /// then the resulting collection is observable.
        /// </returns>
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

        /// <summary>
        /// The convert supports only one way conversion. This method is not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}