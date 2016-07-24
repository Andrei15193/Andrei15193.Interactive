using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// A untility class that dynamically compares two wrapped instances
    /// by a specified property path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There are a number of controls, such as <see cref="Windows.UI.Xaml.Controls.ComboBox"/>
    /// that rely on each item to override <see cref="object.Equals(object)"/> and tell
    /// whether the selected item is in the collection or not.
    /// </para>
    /// <para>
    /// This is problematic. The default implementation of <see cref="object.Equals(object)"/>
    /// uses <see cref="object.ReferenceEquals(object, object)"/> by default, meaning that
    /// if the selected item is not contained by the colletion used as
    /// <see cref="Windows.UI.Xaml.Controls.ItemsControl.ItemsSource"/> then the control
    /// will consider that no item is selected.
    /// </para>
    /// <para>
    /// Overriding <see cref="object.Equals(object)"/> just for making the user interface
    /// controls work as intended cannot be done at the business level. This is only
    /// a UI issue.
    /// </para>
    /// <para>
    /// This class can help solve this issue. One can specify a value and a property path
    /// for that value to use as comparison. The result of <see cref="Equals(DynamicPropertyEquatable)"/>
    /// depends on the values of the specified properties thus when a
    /// <see cref="Windows.UI.Xaml.Controls.ComboBox"/> calls unto <see cref="object.Equals(object)"/>
    /// to determine whether the selected item is part of the items source collection it will
    /// compare by the value of a property and not by reference of items.
    /// </para>
    /// </remarks>
    public class DynamicPropertyEquatable
        : IEquatable<DynamicPropertyEquatable>
    {
        private readonly string _propertyPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicPropertyEquatable"/> class.
        /// </summary>
        /// <param name="value">
        /// The value to use for property navigation and comparsion.
        /// </param>
        /// <param name="propertyPath">
        /// A property path to use when comparing instances. If null is provided then the
        /// propety path of the <see cref="DynamicPropertyEquatable"/> instance that is
        /// compared to will be used.
        /// </param>
        public DynamicPropertyEquatable(object value, string propertyPath = "")
        {
            Value = value;
            _propertyPath = propertyPath;
        }

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">
        ///  An object to compare with this object.
        ///  </param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(DynamicPropertyEquatable other)
            => (other != null && _Equals(other));

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the current object.
        /// </param>
        /// <returns>
        /// true if the specified object is equal to the current object; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
            => Equals(obj as DynamicPropertyEquatable);

        /// <summary>
        /// Determines the hash code for the current object.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            ParameterExpression parameter;
            var propertyAccessor = _GetPropertyAccessor(out parameter, null);
            var getHashCodeMethodInfo = propertyAccessor.Type.GetRuntimeMethod(nameof(GetHashCode), new Type[0]);
            var getHashCodeCall = Expression.Call(propertyAccessor, getHashCodeMethodInfo);
            LambdaExpression getHashCodeExpression;

            if (!propertyAccessor.Type.GetTypeInfo().IsValueType)
            {
                var isPropertyNull = Expression.Equal(propertyAccessor, Expression.Constant(null, propertyAccessor.Type));
                var getHashCodeCallOrZeroIfNull = Expression.Condition(isPropertyNull, Expression.Constant(0), getHashCodeCall);
                getHashCodeExpression = Expression.Lambda(getHashCodeCallOrZeroIfNull, parameter);
            }
            else
                getHashCodeExpression = Expression.Lambda(getHashCodeCall, parameter);

            var getHashCode = getHashCodeExpression.Compile();

            return (int)getHashCode.DynamicInvoke(this);
        }
        private bool _Equals(DynamicPropertyEquatable other)
        {
            ParameterExpression parameterForThis;
            var propertyAccessorForThis = _GetPropertyAccessor(out parameterForThis, other);
            ParameterExpression parameterForOther;
            var propertyAccessorForOther = other._GetPropertyAccessor(out parameterForOther, this);

            var lambda = Expression.Lambda(Expression.Equal(propertyAccessorForThis, propertyAccessorForOther), parameterForThis, parameterForOther);
            var predicate = lambda.Compile();

            return (bool)predicate.DynamicInvoke(this, other);
        }

        private Expression _GetPropertyAccessor(out ParameterExpression parameter, DynamicPropertyEquatable other)
        {
            parameter = Expression.Parameter(typeof(DynamicPropertyEquatable), nameof(DynamicPropertyEquatable));

            Expression accessor;
            if (Value == null)
                accessor = Expression.Constant(null, typeof(object));
            else
            {
                accessor = Expression.Convert(Expression.PropertyOrField(parameter, nameof(Value)), Value.GetType());

                var propertyPath = _propertyPath ?? other?._propertyPath;
                if (!string.IsNullOrWhiteSpace(propertyPath))
                    accessor = propertyPath.Split('.').Aggregate(accessor, Expression.PropertyOrField);
            }

            return accessor;
        }
    }
}