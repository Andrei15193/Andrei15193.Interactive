using System;
using System.Collections;
using Windows.UI.Xaml.Data;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Provides conversion logic to and from <see cref="DynamicPropertyEquatable"/>. If
    /// the converted value is a collection then a <see cref="ProjectedObservableCollection{DynamicPropertyEquatable}"/>
    /// is returned instead to facilitate proper item comparison.
    /// </summary>
    public class DynamicPropertyEquatableConverter
        : IValueConverter
    {
        /// <summary>
        /// Converts the provided <paramref name="value"/> to either a <see cref="DynamicPropertyEquatable"/>
        /// or a <see cref="ProjectedObservableCollection{DynamicPropertyEquatable}"/> in case
        /// of collections.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <param name="targetType">
        /// The target type to convert to.
        /// </param>
        /// <param name="parameter">
        /// An optional parameter representing the property path for returned <see cref="DynamicPropertyEquatable"/>s.
        /// </param>
        /// <param name="language">
        /// The language to use for comparisons.
        /// </param>
        /// <returns>
        /// Returns a <see cref="DynamicPropertyEquatable"/> for the provided <paramref name="value"/>.
        /// In case <paramref name="value"/> is a collection then a <see cref="ProjectedObservableCollection{DynamicPropertyEquatable}"/>
        /// is returned. For each <see cref="DynamicPropertyEquatable"/> the <paramref name="parameter"/>
        /// is used to supply the property path to use for comparison.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var propertyPath = parameter as string;

            var collection = value as IEnumerable;
            if (collection != null)
                return new DynamicPropertyEquatableCollection(item => new DynamicPropertyEquatable(item, propertyPath), collection);
            else
                return new DynamicPropertyEquatable(value, propertyPath);
        }

        /// <summary>
        /// Converts back a privously converted value.
        /// </summary>
        /// <param name="value">
        /// The value to convert back.
        /// </param>
        /// <param name="targetType">
        /// The target type to convert to.
        /// </param>
        /// <param name="parameter">
        /// An optional parameter to use for conversion.
        /// </param>
        /// <param name="language">
        /// The language to use for conversion.
        /// </param>
        /// <returns>
        /// Returns the initial value that was previously converted.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var collection = value as DynamicPropertyEquatableCollection;
            if (collection != null)
                return collection.AsEnumerable();
            else
            {
                var dynamicPropertyEquatable = value as DynamicPropertyEquatable;
                if (dynamicPropertyEquatable != null)
                    return dynamicPropertyEquatable.Value;
            }

            return value;
        }

        private class DynamicPropertyEquatableCollection
            : ProjectedObservableCollection<DynamicPropertyEquatable>
        {
            private readonly IEnumerable _originalCollection;

            internal DynamicPropertyEquatableCollection(Func<object, DynamicPropertyEquatable> selector, IEnumerable collection)
                : base(selector, collection)
            {
                _originalCollection = collection;
            }

            internal IEnumerable AsEnumerable()
                => _originalCollection;
        }
    }
}