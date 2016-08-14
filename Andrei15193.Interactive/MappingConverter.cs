using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml.Data;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Converts based on a given set of <see cref="Mapping"/>s.
    /// </summary>
    public class MappingConverter
        : IValueConverter
    {
        /// <summary>
        /// A collection of <see cref="Mapping"/>s that will be used for conversion.
        /// </summary>
        public List<Mapping> Mappings { get; } = new List<Mapping>();

        /// <summary>
        /// <para>
        /// Looks up and returns <see cref="Mapping.To"/> of the first <see cref="Mapping"/>
        /// from the <see cref="Mappings"/> collection whose <see cref="Mapping.From"/>
        /// property is equal to <paramref name="value"/> and can be assignable to
        /// <paramref name="targetType"/>.
        /// </para>
        /// <para>
        /// In case there is no <see cref="Mapping"/> found then <paramref name="value"/>
        /// is returned.
        /// </para>
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">This parameter is ignored.</param>
        /// <param name="language">This parameter is ignored.</param>
        /// <returns>
        /// Returns the <see cref="Mapping.To"/> value for the first matched <see cref="Mapping"/>
        /// or <paramref name="value"/> if no <see cref="Mapping"/> is found.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter = null, string language = null)
        {
            if (targetType == null)
                targetType = typeof(object);

            var matchedMapping = Mappings.FirstOrDefault(
                mapping => (Equals(mapping.From, value) && _IsTargetTypeMatching(targetType, mapping.To)));
            if (matchedMapping == null)
                return value;
            else
                return matchedMapping.To;
        }

        /// <summary>
        /// <para>
        /// Looks up and returns <see cref="Mapping.From"/> of the first <see cref="Mapping"/>
        /// from the <see cref="Mappings"/> collection whose <see cref="Mapping.To"/>
        /// property is equal to <paramref name="value"/> and can be assignable to
        /// <paramref name="targetType"/>.
        /// </para>
        /// <para>
        /// In case there is no <see cref="Mapping"/> found then <paramref name="value"/>
        /// is returned.
        /// </para>
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">This parameter is ignored.</param>
        /// <param name="language">This parameter is ignored.</param>
        /// <returns>
        /// Returns the <see cref="Mapping.From"/> value for the first matched <see cref="Mapping"/>
        /// or <paramref name="value"/> if no <see cref="Mapping"/> is found.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter = null, string language = null)
        {
            if (targetType == null)
                targetType = typeof(object);

            var matchedMapping = Mappings.FirstOrDefault(
                mapping => (Equals(mapping.To, value)
                && _IsTargetTypeMatching(targetType, mapping.From)));
            if (matchedMapping == null)
                return value;
            else
                return matchedMapping.From;
        }

        private static bool _IsTargetTypeMatching(Type targetType, object value)
        {
            var targetTypeInfo = targetType.GetTypeInfo();

            if (value == null)
                return (!targetTypeInfo.IsValueType || Nullable.GetUnderlyingType(targetType) != null);
            else
                return targetTypeInfo.IsAssignableFrom(value.GetType().GetTypeInfo());
        }
    }
}