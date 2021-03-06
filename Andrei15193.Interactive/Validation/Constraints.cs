﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Andrei15193.Interactive.Validation
{
    /// <summary>
    /// Represents a static container for various <see cref="IConstraint{TValue}"/> for specific types of objects.
    /// </summary>
    [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
    public static class Constraints
    {
        private class ConstraintKey
            : IEquatable<ConstraintKey>
        {
            private readonly string _name;
            private readonly Type _valueType;

            public static bool operator ==(ConstraintKey left, ConstraintKey right)
                => Equals(left, right);
            public static bool operator !=(ConstraintKey left, ConstraintKey right)
                => !Equals(left, right);

            public ConstraintKey(Type valueType, string name)
            {
                if (valueType == null)
                    throw new ArgumentNullException(nameof(valueType));

                _valueType = valueType;
                if (name == null)
                    _name = name;
                else
                    _name = name.Trim();
            }

            public override bool Equals(object obj)
                => Equals(obj as ConstraintKey);
            public bool Equals(ConstraintKey other)
                => other != null
                && _valueType.Equals(other._valueType)
                && StringComparer.OrdinalIgnoreCase.Equals(_name, other._name);

            public override int GetHashCode()
                => _valueType.GetHashCode()
                ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_name ?? string.Empty);
        }

        private static readonly IDictionary<ConstraintKey, object> _constraints
            = new Dictionary<ConstraintKey, object>();
        private static readonly ReaderWriterLockSlim _constraintsLock
            = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Registers the provided <see cref="IConstraint{TValue}"/> for the specified type.
        /// A name can be provided in order to register multiple <see cref="IConstraint{TValue}"/>s to the same type.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="constraint">
        /// The <see cref="IConstraint{TValue}"/> which validate objects of type <typeparamref name="TValue"/>.
        /// </param>
        /// <param name="name">
        /// The name under which to register the <see cref="IConstraint{TValue}"/>, the default is null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="constraint"/> is null.
        /// </exception>
        public static void RegisterFor<TValue>(IConstraint<TValue> constraint, string name)
        {
            if (constraint == null)
                throw new ArgumentNullException(nameof(constraint));

            try
            {
                _constraintsLock.EnterWriteLock();
                _constraints.Add(new ConstraintKey(typeof(TValue), name), constraint);
            }
            finally
            {
                _constraintsLock.ExitWriteLock();
            }
        }
        /// <summary>
        /// Registers the provided <see cref="IConstraint{TValue}"/> for <typeparamref name="TValue"/>.
        /// A name can be provided in order to register multiple <see cref="IConstraint{TValue}"/>s to the same type.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="constraint">
        /// The <see cref="IConstraint{TValue}"/> which validate objects of type <typeparamref name="TValue"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="constraint"/> is null.
        /// </exception>
        public static void RegisterFor<TValue>(IConstraint<TValue> constraint)
            => RegisterFor(constraint, null);

        /// <summary>
        /// If exists, deregisters a previously registerd
        /// <see cref="IConstraint{TValue}"/> with the given <paramref name="name"/>
        /// that matches <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="name">
        /// The name of the <see cref="IConstraint{TValue}"/> to deregister, the default is null.
        /// </param>
        public static void DeregisterFor<TValue>(string name)
        {
            try
            {
                _constraintsLock.EnterWriteLock();
                _constraints.Remove(new ConstraintKey(typeof(TValue), name));
            }
            finally
            {
                _constraintsLock.ExitWriteLock();
            }
        }
        /// <summary>
        /// If exists, deregisters a previously registerd <see cref="IConstraint{TValue}"/> that matches <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        public static void DeregisterFor<TValue>()
            => DeregisterFor<TValue>(null);

        /// <summary>
        /// Retrieves the registered <see cref="IConstraint{TValue}"/> with the provided
        /// <paramref name="name"/> for <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="name">
        /// The name of the <see cref="IConstraint{TValue}"/> to get, the default is null.
        /// </param>
        /// <returns>
        /// Returns the registers <see cref="IConstraint{TValue}"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// The property is retrieved and key does not exist in the collection.
        /// </exception>
        public static IConstraint<TValue> GetFor<TValue>(string name)
        {
            object constraint;
            try
            {
                _constraintsLock.EnterReadLock();
                constraint = _constraints[new ConstraintKey(typeof(TValue), name)];
            }
            finally
            {
                _constraintsLock.ExitReadLock();
            }

            return (IConstraint<TValue>)constraint;
        }
        /// <summary>
        /// Retrieves the registered <see cref="IConstraint{TValue}"/> for <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <returns>
        /// Returns the registers <see cref="IConstraint{TValue}"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// The property is retrieved and key does not exist in the collection.
        /// </exception>
        public static IConstraint<TValue> GetFor<TValue>()
            => GetFor<TValue>(null);

        /// <summary>
        /// Tries to retrieve the registered <see cref="IConstraint{TValue}"/> with the provided
        /// <paramref name="name"/> for <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="name">
        /// The name of the <see cref="IConstraint{TValue}"/> to get, the default is null.
        /// </param>
        /// <returns>
        /// Returns the registers <see cref="IConstraint{TValue}"/>.
        /// </returns>
        public static IConstraint<TValue> TryGetFor<TValue>(string name)
        {
            object constraint;
            try
            {
                _constraintsLock.EnterReadLock();
                if (!_constraints.TryGetValue(new ConstraintKey(typeof(TValue), name), out constraint))
                    constraint = null;
            }
            finally
            {
                _constraintsLock.ExitReadLock();
            }

            return (IConstraint<TValue>)constraint;
        }
        /// <summary>
        /// Tries to retrieve the registered <see cref="IConstraint{TValue}"/> for <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <returns>
        /// Returns the registers <see cref="IConstraint{TValue}"/>.
        /// </returns>
        public static IConstraint<TValue> TryGetFor<TValue>()
            => TryGetFor<TValue>(null);
    }
}