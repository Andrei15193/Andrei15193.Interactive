using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents a default implementation of the <see cref="INotifyPropertyChanged"/>
    /// interface.
    /// </summary>
    public class PropertyChangedNotifier
        : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the given property name.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property that has changed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="propertyName"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="propertyName"/> is an invalid identifier.
        /// </exception>
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            if (!Regex.IsMatch(propertyName, "^[_a-z][_a-z0-9]*$", RegexOptions.IgnoreCase))
                throw new ArgumentException("Must be a valid identifier", nameof(propertyName));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}