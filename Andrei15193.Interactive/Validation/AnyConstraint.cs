using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Andrei15193.Interactive.Validation
{
    /// <summary>
    /// Provides a number of static methods for building <see cref="AnyConstraint"/>s. They
    /// aggregate multiple <see cref="IConstraint{TValue}"/>s and one set of
    /// <see cref="ValidationError"/>s that are returned if all aggregated
    /// <see cref="IConstraint{TValue}"/>s are not satisfied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The aggregated <see cref="IConstraint{TValue}"/> are invoked one at a time, in
    /// the same order they have been provided to the builder, until one that is
    /// satisfied has been found. When this happens all following
    /// <see cref="IConstraint{TValue}"/>s are ignored and it is considered that the
    /// <see cref="AnyConstraint"/> is satisfied. This is similar to evaluating a logical
    /// or expression.
    /// </para>
    /// <para>
    /// In case none of the aggregated <see cref="IConstraint{TValue}"/> are fulfiled,
    /// all <see cref="ValidationError"/>s that have been provided to the builder are
    /// returned in the same order they have been added regardless of which overloads
    /// were used.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// <see cref="AnyConstraint"/> are useful to aggregating multiple constraints in a
    /// similar fashion in which logical or expressions are aggregated. In essence, if
    /// any of the aggregated <see cref="IConstraint{TValue}"/> is fulfiled then no
    /// <see cref="ValidationError"/> is returned. When no <see cref="IConstraint{TValue}"/> 
    /// is satisfied then all <see cref="ValidationError"/> that have been returned from
    /// any of the aggregated <see cref="IConstraint{TValue}"/> are ignored and only the
    /// ones provided to the <see cref="AnyConstraint"/> builder are returned.
    /// </para>
    /// <code>
    /// var constraint = AnyConstraint
    ///     .From&lt;string&gt;(value =&gt; value == null)
    ///     .Or(value =&gt; value.StartsWith("Test"))
    ///     .Return(new ValidationError("Value must be either null or start with \"Test\""));
    /// 
    /// var validationErrors = await constraint.CheckAsync("not valid");
    /// 
    /// Assert.AreEqual(
    ///     "Value must be either null or start with \"Test\"",
    ///     validationErrors.Single().Text,
    ///     ignoreCase: false);
    /// </code>
    /// </example>
    public static class AnyConstraint
    {
        /// <summary>
        /// Represents a predicate for checking whether value satisfies a constraint.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="value">
        /// The value to check whether it satisfies the constraint.
        /// </param>
        /// <returns>
        /// Returns true if the constraint is satisfied; false otherwise.
        /// </returns>
        public delegate bool Predicate<TValue>(TValue value);

        /// <summary>
        /// Represents an asynchronous predicate for checking whether value satisfies a constraint.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="value">
        /// The value to check whether it satisfies the constraint.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// Returns an awaitable task which in turn results in true if the constraint is
        /// satisfied; false otherwise.
        /// </returns>
        public delegate Task<bool> AsyncPredicate<TValue>(TValue value, CancellationToken cancellationToken);

        /// <summary>
        /// Represents a function that provides just one <see cref="ValidationError"/>.
        /// </summary>
        /// <returns>
        /// Returns a single <see cref="ValidationError"/>.
        /// </returns>
        public delegate ValidationError ValidationErrorProvider();

        /// <summary>
        /// Represents a function that provides a number of <see cref="ValidationError"/>s.
        /// </summary>
        /// <returns>
        /// Returns a number of <see cref="ValidationError"/>s.
        /// </returns>
        public delegate IEnumerable<ValidationError> ValidationErrorsProvider();

        /// <summary>
        /// Initializes an <see cref="IAnyConstraintBuilder{TValue}"/> with the provided <see cref="IConstraint{TValue}"/>.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="constraint">
        /// The constraint to initialize the builder with.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IAnyConstraintBuilder{TValue}"/> that can be used to construct an <see cref="AnyConstraint"/>.
        /// </returns>
        public static IAnyConstraintBuilder<TValue> From<TValue>(IConstraint<TValue> constraint)
            => new AnyConstraintBuilder<TValue>(constraint);

        /// <summary>
        /// Creates an <see cref="IConstraint{TValue}"/> from the provided <see cref="Predicate{TValue}"/> and initializes an <see cref="IAnyConstraintBuilder{TValue}"/> with it.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="predicate">
        /// A predicate that indicates whether the object satisfies a constraint.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IAnyConstraintBuilder{TValue}"/> that can be used to construct an <see cref="AnyConstraint"/>.
        /// </returns>
        public static IAnyConstraintBuilder<TValue> From<TValue>(Predicate<TValue> predicate)
            => new AnyConstraintBuilder<TValue>(_GetConstraintFrom(predicate));

        /// <summary>
        /// Creates an <see cref="IConstraint{TValue}"/> from the provided <see cref="AsyncPredicate{TValue}"/> and initializes an <see cref="IAnyConstraintBuilder{TValue}"/> with it.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="asyncPredicate">
        /// An asynchronous predicate that indicates whether the object satisfies a constraint.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IAnyConstraintBuilder{TValue}"/> that can be used to construct an <see cref="AnyConstraint"/>.
        /// </returns>
        public static IAnyConstraintBuilder<TValue> From<TValue>(AsyncPredicate<TValue> asyncPredicate)
            => new AnyConstraintBuilder<TValue>(_GetConstraintFrom(asyncPredicate));

        /// <summary>
        /// Represents the interface of an <see cref="AnyConstraint"/> builder which allows adding
        /// <see cref="IConstraint{TValue}"/>s and eventually transitioning to a builder
        /// which allows adding multiple <see cref="ValidationError"/>s and eventually creating
        /// the <see cref="IConstraint{TValue}"/>.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        public interface IAnyConstraintBuilder<TValue>
        {
            /// <summary>
            /// Adds an <see cref="IConstraint{TValue}"/> to the builder.
            /// </summary>
            /// <param name="constraint">
            /// The <see cref="IConstraint{TValue}"/> to add.
            /// </param>
            /// <returns>
            /// Returns the builder whose method has been called.
            /// </returns>
            IAnyConstraintBuilder<TValue> Or(IConstraint<TValue> constraint);

            /// <summary>
            /// Adds an <see cref="IConstraint{TValue}"/> that is created from the provided <paramref name="predicate"/>.
            /// </summary>
            /// <param name="predicate">
            /// A predicate that indicates whether the object satisfies a constraint.
            /// </param>
            /// <returns>
            /// Returns the builder whose method has been called.
            /// </returns>
            IAnyConstraintBuilder<TValue> Or(Predicate<TValue> predicate);

            /// <summary>
            /// Adds an <see cref="IConstraint{TValue}"/> that is created from the provided <paramref name="asyncPredicate"/>.
            /// </summary>
            /// <param name="asyncPredicate">
            /// An asynchronous predicate that indicates whether the object satisfies a constraint.
            /// </param>
            /// <returns>
            /// Returns the builder whose method has been called.
            /// </returns>
            IAnyConstraintBuilder<TValue> Or(AsyncPredicate<TValue> asyncPredicate);

            /// <summary>
            /// Constructs an <see cref="AnyConstraint"/> from all aggregated <see cref="IConstraint{TValue}"/>s
            /// which returns the provided <paramref name="validationError"/> when none of them are satisfied.
            /// </summary>
            /// <param name="validationError">
            /// The <see cref="ValidationError"/> to return when no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </param>
            /// <returns>
            /// Returns an <see cref="IConstraint{TValue}"/> that returns the provided <paramref name="validationError"/>
            /// when no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </returns>
            IConstraint<TValue> Return(ValidationError validationError);

            /// <summary>
            /// Constructs an <see cref="AnyConstraint"/> from all aggregated <see cref="IConstraint{TValue}"/>s
            /// which returns the provided <paramref name="validationErrors"/> when none of them are satisfied.
            /// </summary>
            /// <param name="validationErrors">
            /// The <see cref="ValidationError"/>s to return when no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </param>
            /// <returns>
            /// Returns an <see cref="IConstraint{TValue}"/> that returns the provided <paramref name="validationErrors"/>
            /// when no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </returns>
            IConstraint<TValue> Return(IEnumerable<ValidationError> validationErrors);

            /// <summary>
            /// Constructs an <see cref="AnyConstraint"/> from all aggregated <see cref="IConstraint{TValue}"/>s
            /// which returns the provided <paramref name="validationErrors"/> when none of them are satisfied.
            /// </summary>
            /// <param name="validationErrors">
            /// The <see cref="ValidationError"/>s to return when no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </param>
            /// <returns>
            /// Returns an <see cref="IConstraint{TValue}"/> that returns the provided <paramref name="validationErrors"/>
            /// when no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </returns>
            IConstraint<TValue> Return(params ValidationError[] validationErrors);

            /// <summary>
            /// Constructs an <see cref="AnyConstraint"/> from all aggregated <see cref="IConstraint{TValue}"/>s
            /// which returns the <see cref="ValidationError"/> provided by <paramref name="validationErrorProvider"/>
            /// when none of them are satisfied.
            /// </summary>
            /// <param name="validationErrorProvider">
            /// The <see cref="ValidationErrorProvider"/> that provides the <see cref="ValidationError"/> when
            /// no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </param>
            /// <returns>
            /// Returns an <see cref="IConstraint{TValue}"/> that returns the <see cref="ValidationError"/> provided by
            /// <paramref name="validationErrorProvider"/> when no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </returns>
            IConstraint<TValue> Return(ValidationErrorProvider validationErrorProvider);

            /// <summary>
            /// Constructs an <see cref="AnyConstraint"/> from all aggregated <see cref="IConstraint{TValue}"/>s
            /// which returns the <see cref="ValidationError"/>s provided by <paramref name="validationErrorsProvider"/>
            /// when none of them are satisfied.
            /// </summary>
            /// <param name="validationErrorsProvider">
            /// The <see cref="ValidationErrorProvider"/> that provides the <see cref="ValidationError"/>s when
            /// no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </param>
            /// <returns>
            /// Returns an <see cref="IConstraint{TValue}"/> that returns all <see cref="ValidationError"/>s provided by
            /// <paramref name="validationErrorsProvider"/> when no aggregated <see cref="IConstraint{TValue}"/> is satisfied.
            /// </returns>
            IConstraint<TValue> Return(ValidationErrorsProvider validationErrorsProvider);
        }

        private static readonly IEnumerable<ValidationError> _defaultValidationError =
            Enumerable.Repeat(new ValidationError("The is an error"), 1);

        private static IConstraint<TValue> _GetConstraintFrom<TValue>(Predicate<TValue> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return Constraint.From<TValue>(
                value => predicate(value)
                ? Enumerable.Empty<ValidationError>()
                : _defaultValidationError);
        }
        private static IConstraint<TValue> _GetConstraintFrom<TValue>(AsyncPredicate<TValue> asyncPredicate)
        {
            if (asyncPredicate == null)
                throw new ArgumentNullException(nameof(asyncPredicate));

            return Constraint.From<TValue>(
                async (value, cancellationToken) =>
                {
                    var task = asyncPredicate(value, cancellationToken);
                    if (task == null)
                        return Enumerable.Empty<ValidationError>();

                    return await task
                        ? Enumerable.Empty<ValidationError>()
                        : _defaultValidationError;
                });
        }

        private sealed class AnyConstraintBuilder<TValue>
            : IAnyConstraintBuilder<TValue>
        {
            private sealed class AnyConstraint
                : Constraint<TValue>
            {
                private readonly IEnumerable<IConstraint<TValue>> _constraints;
                private readonly IEnumerable<ValidationError> _validationErrors;
                private readonly ValidationErrorProvider _validationErrorProvider;
                private readonly ValidationErrorsProvider _validationErrorsProvider;

                public AnyConstraint(IEnumerable<IConstraint<TValue>> constraints, ValidationError validationError)
                {
                    if (constraints == null)
                        throw new ArgumentNullException(nameof(constraints));
                    if (constraints.Any(constraint => constraint == null))
                        throw new ArgumentException(
                            "Must not contain any null constraints",
                            nameof(constraints));
                    if (validationError == null)
                        throw new ArgumentNullException(nameof(validationError));

                    _constraints = constraints;
                    _validationErrors = new[] { validationError };
                }
                public AnyConstraint(IEnumerable<IConstraint<TValue>> constraints, IEnumerable<ValidationError> validationErrors)
                {
                    if (constraints == null)
                        throw new ArgumentNullException(nameof(constraints));
                    if (constraints.Any(constraint => constraint == null))
                        throw new ArgumentException(
                            "Must not contain any null constraints",
                            nameof(constraints));

                    if (validationErrors == null)
                        throw new ArgumentNullException(nameof(validationErrors));

                    _constraints = constraints;
                    _validationErrors = validationErrors.Where(validationError => validationError != null).ToList();
                }
                public AnyConstraint(IEnumerable<IConstraint<TValue>> constraints, ValidationErrorProvider validationErrorProvider)
                {
                    if (constraints == null)
                        throw new ArgumentNullException(nameof(constraints));
                    if (constraints.Any(constraint => constraint == null))
                        throw new ArgumentException(
                            "Must not contain any null constraints",
                            nameof(constraints));
                    if (validationErrorProvider == null)
                        throw new ArgumentNullException(nameof(validationErrorProvider));

                    _constraints = constraints;
                    _validationErrorProvider = validationErrorProvider;
                }
                public AnyConstraint(IEnumerable<IConstraint<TValue>> constraints, ValidationErrorsProvider validationErrorsProvider)
                {
                    if (constraints == null)
                        throw new ArgumentNullException(nameof(constraints));
                    if (constraints.Any(constraint => constraint == null))
                        throw new ArgumentException(
                            "Must not contain any null constraints",
                            nameof(constraints));
                    if (validationErrorsProvider == null)
                        throw new ArgumentNullException(nameof(validationErrorsProvider));

                    _constraints = constraints;
                    _validationErrorsProvider = validationErrorsProvider;
                }

                protected override async Task<IEnumerable<ValidationError>> OnCheckAsync(TValue value, CancellationToken cancellationToken)
                {
                    if (await _HasProblem(value, cancellationToken))
                        if (_validationErrors != null)
                            return _validationErrors;
                        else if (_validationErrorProvider != null)
                        {
                            var validationError = _validationErrorProvider();
                            if (validationError == null)
                                return null;
                            else
                                return new[] { _validationErrorProvider() };
                        }
                        else
                            return _validationErrorsProvider();
                    else
                        return null;
                }

                private async Task<bool> _HasProblem(TValue value, CancellationToken cancellationToken)
                {
                    var hasProblem = true;

                    using (var constraint = _constraints.GetEnumerator())
                        while (hasProblem && constraint.MoveNext())
                        {
                            var task = constraint.Current.CheckAsync(value, cancellationToken);
                            if (task == null)
                                hasProblem = false;
                            else
                            {
                                var validationErrors = await task;
                                hasProblem = validationErrors.Any();
                            }
                        }

                    return hasProblem;
                }
            }

            private readonly ICollection<IConstraint<TValue>> _constraints;

            public AnyConstraintBuilder(IConstraint<TValue> constraint)
            {
                _constraints = new List<IConstraint<TValue>>();

                _Add(constraint);
            }
            private void _Add(IConstraint<TValue> constraint)
            {
                if (constraint == null)
                    throw new ArgumentNullException(nameof(constraint));

                _constraints.Add(constraint);
            }

            public IAnyConstraintBuilder<TValue> Or(IConstraint<TValue> constraint)
            {
                _Add(constraint);
                return this;
            }
            public IAnyConstraintBuilder<TValue> Or(Predicate<TValue> predicate)
                => Or(_GetConstraintFrom(predicate));
            public IAnyConstraintBuilder<TValue> Or(AsyncPredicate<TValue> asyncPredicate)
                => Or(_GetConstraintFrom(asyncPredicate));

            public IConstraint<TValue> Return(ValidationError validationError)
                => new AnyConstraint(_constraints, validationError);

            public IConstraint<TValue> Return(IEnumerable<ValidationError> validationErrors)
                => new AnyConstraint(_constraints, validationErrors);

            public IConstraint<TValue> Return(params ValidationError[] validationErrors)
                => Return(validationErrors.AsEnumerable());

            public IConstraint<TValue> Return(ValidationErrorProvider validationErrorProvider)
                => new AnyConstraint(_constraints, validationErrorProvider);

            public IConstraint<TValue> Return(ValidationErrorsProvider validationErrorsProvider)
                => new AnyConstraint(_constraints, validationErrorsProvider);
        }
    }
}